using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Audio;
using VNBase.UI;
using VNBase.Assets;
using Script = VNScript.Script;
using Sound = VNBase.Assets.Sound;

namespace VNBase;

public sealed partial class ScriptPlayer
{
	/// <summary>
	/// Current text segment index within the active label
	/// </summary>
	private int _currentTextIndex;
	
	private async void SetLabel( Script.Label label )
	{
		try
		{
			// Clean up any existing text effect before starting a new one
			SkipDialogueEffect();
			ActiveLabel = label;
			_currentTextIndex = 0; // Reset text index when setting new label
			
			if ( LoggingEnabled )
			{
				Log.Info( $"Loading Label {label.Name}" );
			}
			
			// Execute code blocks BEFORE processing dialogues
			// This ensures variables are set before dialogue tries to reference them
			if ( label.AfterLabel?.CodeBlocks is not null )
			{
				var environment = ActiveScript?.GetEnvironment() ?? _environment;
				
				foreach ( var codeBlock in label.AfterLabel.CodeBlocks )
				{
					try
					{
						codeBlock.Execute( environment );
					}
					catch ( Exception e )
					{
						Log.Error( $"Error executing code block in label {label.Name}: {e.Message}" );
					}
				}
			}
			
			State.Characters.Clear();
			label.Characters.ForEach( State.Characters.Add );
			
			foreach ( var sound in label.Assets.OfType<Sound>() )
			{
				State.Sounds.Add( sound );
				
				if ( sound is Music )
				{
					State.BackgroundMusic = MusicPlayer.Play( FileSystem.Mounted, sound.EventName );
					State.BackgroundMusic.TargetMixer = Mixer.FindMixerByName( "Music" );
				}
				else
				{
					if ( string.IsNullOrEmpty( sound.MixerName ) )
					{
						sound.Play();
					}
					else
					{
						sound.Play( sound.MixerName );
					}
				}
				
				if ( LoggingEnabled )
				{
					Log.Info( $"Played SoundAsset {sound} from label {label.Name}" );
				}
			}
			
			try
			{
				State.Background = label.Assets.OfType<Background>().SingleOrDefault()?.Path;
			}
			catch ( InvalidOperationException )
			{
				Log.Error( $"There can only be one {nameof(Background)} in label {label.Name}!" );
				State.Background = null;
			}
			
			if ( _currentTextIndex == 0 )
			{
				OnLabelSet?.Invoke( label );
			}
			
			// Display the current text segment
			await DisplayCurrentTextSegment();
		}
		catch ( Exception e )
		{
			Log.Error( e.Message );
		}
	}
	
	private async Task DisplayCurrentTextSegment()
	{
		if ( ActiveLabel is null || ActiveLabel.Dialogues.Count == 0 )
		{
			// No dialogues to display - go straight to after label logic
			if ( ActiveLabel?.AfterLabel is not null )
			{
				ExecuteAfterLabel();
			}
			
			return;
		}
		
		if ( _currentTextIndex >= ActiveLabel.Dialogues.Count )
		{
			Log.Error( $"Text index {_currentTextIndex} out of range for label {ActiveLabel.Name}" );
			return;
		}
		
		var activeDialogue = ActiveLabel.Dialogues[_currentTextIndex];
		State.SpeakingCharacter = activeDialogue.Speaker;
		
		// Use the same environment as code blocks
		var environment = ActiveScript?.GetEnvironment() ?? _environment;
		
		if ( Settings.TextEffectEnabled )
		{
			_cts = new CancellationTokenSource();
			
			try
			{
				var formattedText = activeDialogue.Text.Format( environment );
				await Settings.TextEffect.Play( formattedText, (int)Settings.TextEffectSpeed, UpdateDialogueText, _cts.Token );
				EndDialogue( activeDialogue, ActiveLabel );
			}
			catch ( OperationCanceledException )
			{
				EndDialogue( activeDialogue, ActiveLabel );
			}
		}
		else
		{
			// Skip the text effect entirely
			EndDialogue( activeDialogue, ActiveLabel );
		}
	}
	
	/// <summary>
	/// Advances to the next text segment in the current label, or executes AfterLabel if there are no more segments
	/// </summary>
	// ReSharper disable once MemberCanBePrivate.Global
	public void AdvanceText()
	{
		if ( ActiveLabel is null )
		{
			ExecuteAfterLabel();
			return;
		}
		
		_currentTextIndex++;
		OnTextAdvanced?.Invoke( _currentTextIndex );
		
		// If we have more text segments, display the next one
		if ( _currentTextIndex < ActiveLabel.Dialogues.Count )
		{
			_ = DisplayCurrentTextSegment();
		}
		else
		{
			// No more text segments
			ExecuteAfterLabel();
		}
	}
	
	private void ExecuteAfterLabel()
	{
		if ( ActiveScript is null || ActiveLabel is null )
		{
			Log.Error( $"Unable to execute the AfterLabel, there is either no active script or label!" );
			
			return;
		}
		
		var afterLabel = ActiveLabel.AfterLabel;
		
		if ( afterLabel is null )
		{
			return;
		}
		
		foreach ( var sound in State.Sounds.ToArray() )
		{
			sound.Stop();
			State.Sounds.Remove( sound );
		}
		
		// Do not let us continue if there is an empty input box.
		var hasInput = ActiveLabel.ActiveInput is not null;
		
		if ( hasInput && Hud is not null )
		{
			var input = Hud.GetSubPanel<TextInput>();
			
			if ( input is null )
			{
				return;
			}
			
			if ( string.IsNullOrWhiteSpace( input.Entry?.Text ) )
			{
				return;
			}
		}
		
		if ( afterLabel.IsLastLabel )
		{
			UnloadScript();
			return;
		}
		
		if ( !string.IsNullOrEmpty( afterLabel.ScriptPath ) )
		{
			LoadScript( afterLabel.ScriptPath );
			return;
		}
		
		if ( afterLabel.TargetLabel is null )
		{
			return;
		}
		
		if ( _activeDialogue is null )
		{
			Log.Error( "There is no active dialogue set, unable to switch active labels!" );
			return;
		}
		
		SetLabel( _activeDialogue.Labels[afterLabel.TargetLabel] );
	}
	
	private async void EndDialogue( Script.Dialogue dialogue, Script.Label label )
	{
		try
		{
			if ( ActiveScript is null || ActiveLabel is null )
			{
				return;
			}
			
			// Use the same environment as code blocks
			var environment = ActiveScript.GetEnvironment();
			
			// Check if this is the last dialogue in the label
			var isLastDialogue = _currentTextIndex >= ActiveLabel.Dialogues.Count - 1;
			
			// If we are in Automatic Mode and there are no choices, check if we should auto-advance
			if ( IsAutomaticMode && label.Choices.Count == 0 && !isLastDialogue )
			{
				try
				{
					await Task.DelaySeconds( Settings.AutoLabelDelay );
					
					// Auto-advance to next text segment or after label
					AdvanceText();
					
					return;
				}
				catch ( OperationCanceledException )
				{
					State.IsDialogueFinished = false;
				}
			}
			
			var formattedText = dialogue.Text.Format( environment );
			if ( State.DialogueText != formattedText )
			{
				State.DialogueText = formattedText;
			}
			
			// Only set choices if this is the last dialogue
			if ( isLastDialogue )
			{
				State.Choices = ActiveLabel.Choices;
			}
			else
			{
				State.Choices = [];
			}
			
			State.IsDialogueFinished = true;
			AddToDialogueHistory( dialogue, label );
		}
		catch ( Exception e )
		{
			Log.Error( e.Message );
		}
	}
	
	private void UpdateDialogueText( string text )
	{
		State.DialogueText = text;
		State.IsDialogueFinished = false;
	}
}
