using Sandbox;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using VNBase.Assets;
using VNBase.UI;
using SandLang;

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
	public Script? ActiveScript { get; private set; }

	/// <summary>
	/// The currently active script label.
	/// </summary>
	public Dialogue.Label? ActiveLabel { get; private set; }

	/// <summary>
	/// If there is an active playing script.
	/// </summary>
	[Property]
	public bool IsScriptActive { get; private set; }

	/// <summary>
	/// If not empty, will load the script at this path on initial component start.
	/// </summary>
	[Property, Group( "Script" ), FilePath( Extension = "vnscript" )]
	public string? InitialScript { get; set; }

	/// <summary>
	/// The <see cref="ScriptState"/>.
	/// </summary>
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
	public Settings? Settings { get; set; }

	[Property, RequireComponent, Group( "Components" )]
	public VNHud? Hud { get; set; }

	private Dialogue? _activeDialogue;
	private CancellationTokenSource? _cts;

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

	private bool SkipActionPressed => Settings?.SkipActions.Any( x => x.Pressed ) ?? false;

	protected override void OnUpdate()
	{
		if ( ActiveScript is null || ActiveLabel is null )
		{
			return;
		}

		if ( !Settings?.SkipActionEnabled ?? false )
		{
			return;
		}

		if ( SkipActionPressed )
		{
			if ( !State.IsDialogueFinished )
			{
				SkipDialogueEffect();
			}
			else if ( State.Choices.Count == 0 )
			{
				ExecuteAfterLabel();
			}
		}
		else if ( IsAutomaticMode )
		{
			if ( State is { IsDialogueFinished: true, Choices.Count: 0 } )
			{
				ExecuteAfterLabel();
			}
		}
	}

	/// <summary>
	/// Read and load the script at the provided path.
	/// </summary>
	/// <param name="path">Path to the script to load.</param>
	public void LoadScript( string path )
	{
		var dialogue = FileSystem.Mounted.ReadAllText( path );

		if ( dialogue is null )
		{
			Log.Error( $"Unable to load script! Script file couldn't be found by path: {path}" );
			return;
		}

		if ( !string.IsNullOrEmpty( dialogue ) )
		{
			Script script = new( path );
			LoadScript( script );
		}
		else
		{
			Log.Error( "Unable to load script! The script file is empty." );
		}
	}

	/// <summary>
	/// Load the provided Script object.
	/// </summary>
	/// <param name="script">Script to load.</param>
	public void LoadScript( Script script )
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

		if ( Settings?.StopMusicPlaybackOnUnload ?? true )
		{
			// Stop all previously playing songs.
			foreach ( var music in State.Sounds.OfType<Music>() )
			{
				music.Stop();
			}
		}

		ActiveScript = script;
		_activeDialogue = ActiveScript.ParseDialogue();

		script.OnLoad();
		OnScriptLoad?.Invoke( script );
		
		SetEnvironment( _activeDialogue );
		SetLabel( _activeDialogue.InitialLabel );
		
		IsScriptActive = true;
	}

	/// <summary>
	/// Unloads the currently active script.
	/// </summary>
	public void UnloadScript()
	{
		if ( ActiveScript is null || ActiveLabel is null )
		{
			return;
		}

		// Safety check. Should hopefully not cause issues.
		if ( ActiveScript.OnChoiceSelected is not null )
		{
			foreach ( var @delegate in ActiveScript.OnChoiceSelected.GetInvocationList() )
			{
				ActiveScript.OnChoiceSelected -= (Action<Dialogue.Choice>)@delegate;
			}
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

	/// <summary>
	/// Skip the currently active text effect.
	/// </summary>
	public void SkipDialogueEffect()
	{
		if ( ActiveScript is null || ActiveLabel is null )
		{
			return;
		}

		if ( IsAutomaticMode )
		{
			return;
		}
		
		_cts?.Cancel();
		_cts?.Dispose();
		_cts = null;
	}
}
