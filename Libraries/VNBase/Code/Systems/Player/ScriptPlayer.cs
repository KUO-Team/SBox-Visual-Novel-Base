using Sandbox;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using VNBase.UI;
using Script = VNScript.Script;

namespace VNBase;

/// <summary>
/// Responsible for handling visual novel base scripts.
/// </summary>
[Title( "VN Script Player" )]
[Category( "VNBase" )]
[Icon( "menu_book" )]
public sealed partial class ScriptPlayer : Component
{
	/// <summary>
	/// The currently active script.
	/// </summary>
	public Assets.Script? ActiveScript { get; private set; }
	
	/// <summary>
	/// The currently active script label.
	/// </summary>
	public Script.Label? ActiveLabel { get; private set; }
	
	/// <summary>
	/// If there is an active playing script.
	/// </summary>
	[Property]
	public bool IsScriptActive { get; private set; }
	
	/// <summary>
	/// If not empty, will load the script asset at this path on initial component start.
	/// </summary>
	[Property, Group( "Script" ), FilePath( Extension = SupportedExtensions )]
	public string? InitialScript { get; set; }
	
	/// <summary>
	/// The active <see cref="ScriptState"/>.
	/// </summary>
	/// <seealso cref="ScriptState"/>
	[Property, Group( "Script" )]
	public ScriptState State { get; } = new();
	
	/// <summary>
	/// Automatic mode moves through dialogues without choices automatically.
	/// </summary>
	[Property, Group( "Dialogue" )]
	public bool IsAutomaticMode { get; set; }
	
	/// <summary>
	/// If automatic mode can be enabled.
	/// </summary>
	[Property, Group( "Dialogue" )]
	public bool IsAutomaticModeAvailable { get; set; } = true;
	
	[Property, RequireComponent, Group( "Components" )]
	public VNHud? Hud { get; set; }
	
	[Property]
	public Settings Settings { get; } = new();
	
	private Script? _activeDialogue;
	private CancellationTokenSource? _playbackCts;
	private int _playbackRevision;
	
	/// <summary>
	/// Value-separated by comma, list of supported script files by extension.
	/// </summary>
	private const string SupportedExtensions = "vnscript,json";
	
	protected override void OnStart()
	{
		if ( !string.IsNullOrEmpty( InitialScript ) )
		{
			LoadScript( InitialScript );
		}
		
		if ( !Scene.GetAllComponents<VNHud>().Any() )
		{
			Log.Warning( "No VNHud Component found, ScriptPlayer will not be immediately visible!" );
		}
	}
	
	private bool SkipActionPressed => Settings.SkipActions.Any( x => x.Pressed );
	
	protected override void OnUpdate()
	{
		if ( ActiveScript is null || ActiveLabel is null )
		{
			return;
		}
		
		if ( !Settings.SkipActionEnabled )
		{
			return;
		}
		
		if ( SkipActionPressed )
		{
			AdvanceOrSkipDialogueEffect();
		}
	}
	
	/// <summary>
	/// Attempts to read the script from the provided script asset.
	/// </summary>
	/// <param name="script">The script asset to read from.</param>
	/// <param name="dialogue">When this method returns, contains the script file contents if found, or an empty string if not.</param>
	/// <returns><see langword="true"/> if the script file was found and read successfully; otherwise <see langword="false"/>.</returns>
	private static bool TryReadScript( Assets.Script script, out string dialogue )
	{
		if ( FileSystem.Mounted.FileExists( script.Path ) )
		{
			dialogue = FileSystem.Mounted.ReadAllText( script.Path );
			return true;
		}
		
		dialogue = string.Empty;
		return false;
	}
	
	/// <summary>
	/// Read and load the script at the provided path.
	/// </summary>
	/// <param name="path">Path to the script to load.</param>
	// ReSharper disable once MemberCanBePrivate.Global
	public void LoadScript( string path )
	{
		var extension = Path.GetExtension( path ).ToLowerInvariant();
		
		var script = extension == ".json"
			? new Assets.JsonScript( path )
			: new Assets.Script( path );
		
		if ( !TryReadScript( script, out var dialogue ) )
		{
			Log.Error( $"Unable to load script! File not found: {path}" );
			return;
		}
		
		if ( string.IsNullOrEmpty( dialogue ) )
		{
			Log.Error( "Unable to load script! The script file is empty." );
			return;
		}
		
		LoadScript( script );
	}
	
	/// <summary>
	/// Load the provided Script object.
	/// </summary>
	/// <param name="script">Script to load.</param>
	// ReSharper disable once MemberCanBePrivate.Global
	public void LoadScript( Assets.Script script )
	{
		var scriptName = string.Empty;
		
		if ( script.FromFile )
		{
			scriptName = Path.GetFileNameWithoutExtension( script.Path );
		}
		
		if ( LoggingEnabled )
		{
			Log.Info( $"Loading script: {scriptName}" );
		}
		
		ActiveScript = script;
		_activeDialogue = ActiveScript.Parse();
		script.OnLoad();
		OnScriptLoad?.Invoke( script );
		SetEnvironment( _activeDialogue );
		SetLabel( _activeDialogue.InitialLabel );
		IsScriptActive = true;
	}
	
	/// <summary>
	/// Unloads the currently active script.
	/// </summary>
	// ReSharper disable once MemberCanBePrivate.Global
	public void UnloadScript()
	{
		if ( ActiveScript is null || ActiveLabel is null )
		{
			return;
		}
		
		CancelPlaybackOperation();
		_isSkipping = false;
		
		// Safety check. Should hopefully not cause issues.
		if ( ActiveScript.OnChoiceSelected is not null )
		{
			foreach ( var @delegate in ActiveScript.OnChoiceSelected.GetInvocationList() )
			{
				ActiveScript.OnChoiceSelected -= (Action<Script.Choice>)@delegate;
			}
		}
		
		if ( Settings.StopMusicPlaybackOnUnload )
		{
			State.StopBackgroundMusic();
		}
		
		State.Clear();
		ActiveScript.OnUnload();
		OnScriptUnload?.Invoke( ActiveScript );
		IsScriptActive = false;
		
		var nextScript = ActiveScript.NextScript;
		if ( nextScript is not null )
		{
			LoadScript( nextScript );
		}
		else
		{
			ActiveScript = null;
		}
		
		if ( LoggingEnabled )
		{
			Log.Info( $"Unloaded active script." );
		}
	}
	
	private int StartPlaybackOperation( out CancellationToken cancellationToken )
	{
		CancelPlaybackOperation();
		
		_playbackCts = new CancellationTokenSource();
		cancellationToken = _playbackCts.Token;
		return _playbackRevision;
	}
	
	private void CancelPlaybackOperation()
	{
		_playbackRevision++;
		
		_playbackCts?.Cancel();
		_playbackCts?.Dispose();
		_playbackCts = null;
	}
	
	/// <summary>
	/// Skip the currently active text effect.
	/// </summary>
	// ReSharper disable once MemberCanBePrivate.Global
	public void SkipDialogueEffect()
	{
		if ( ActiveScript is null || ActiveLabel is null )
		{
			return;
		}
		
		_playbackCts?.Cancel();
	}
	
	/// <summary>
	/// If the dialogue isn't finished, skip the effect, otherwise just advance if we can.
	/// </summary>
	public void AdvanceOrSkipDialogueEffect()
	{
		if ( !State.IsDialogueFinished )
		{
			SkipDialogueEffect();
		}
		else if ( State.Choices.Count == 0 )
		{
			CancelPlaybackOperation();
			AdvanceText();
		}
	}
}
