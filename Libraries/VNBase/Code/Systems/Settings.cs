using Sandbox;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using static VNBase.Effects;

namespace VNBase;

/// <summary>
/// Settings for the script player.
/// </summary>
[Title( "VN Settings" )]
[Category( "VNBase" )]
[Icon( "settings" )]
public class Settings : Component
{
	[Property, ToggleGroup( "TextEffectEnabled" )]
	public bool TextEffectEnabled { get; set; } = true;

	/// <summary>
	/// The currently active text effect.
	/// </summary>
	[JsonIgnore]
	[Property, ToggleGroup( "TextEffectEnabled" )]
	public ITextEffect TextEffect { get; set; } = new Typewriter();

	/// <summary>
	/// Time used for the active text effect to determine text delays.
	/// </summary>
	[Property, ToggleGroup( "TextEffectEnabled" )]
	public TextSpeed TextEffectSpeed { get; set; } = TextSpeed.Normal;

	/// <summary>
	/// The actions to skip the currently active text effect.
	/// By default, this is set to "jump".
	/// </summary>
	[InlineEditor]
	[Property, ToggleGroup( "SkipActionEnabled" )]
	public List<Input> SkipActions { get; set; } = ["jump"];

	/// <summary>
	/// If we are able to skip the active text effect using a skip action.
	/// </summary>
	[Property, ToggleGroup( "SkipActionEnabled" )]
	public bool SkipActionEnabled { get; set; } = true;

	/// <summary>
	/// The Inputs to show the history UI.
	/// </summary>
	[InlineEditor]
	[Title( "History Inputs" )]
	[Property, Group( "Actions" )]
	public List<Input> HistoryInputs { get; set; } = [];

	/// <summary>
	/// The Inputs to show the settings UI.
	/// </summary>
	[InlineEditor]
	[Title( "Settings Inputs" )]
	[Property, Group( "Actions" )]
	public List<Input> SettingsInputs { get; set; } = [];

	/// <summary>
	/// The Inputs to toggle the UI.
	/// </summary>
	[InlineEditor]
	[Title( "Hide UI Inputs" )]
	[Property, Group( "Actions" )]
	// ReSharper disable once InconsistentNaming
	public List<Input> HideUIInputs { get; set; } = [];

	/// <summary>
	/// When a script is unloaded, should we end all music playback from it?
	/// </summary>
	[Property, Group( "Audio" )]
	public bool StopMusicPlaybackOnUnload { get; set; } = true;

	/// <summary>
	/// If we should show the control panel.
	/// </summary>
	[Property]
	public bool ControlPanelEnabled { get; set; } = true;

	/// <summary>
	/// If we should show the settings UI.
	/// If your game implements its own, you can disable this.
	/// </summary>
	[Property]
	public bool SettingsEnabled { get; set; } = true;

	/// <summary>
	/// Path to the background image assets.
	/// </summary>
	public const string BackgroundsPath = "/materials/scripts/backgrounds/";

	/// <summary>
	/// Path to character portrait images.
	/// </summary>
	public const string CharacterPortraitsPath = "/materials/scripts/";

	/// <summary>
	/// Path to the character resources.
	/// </summary>
	public const string CharacterResourcesPath = "/characters/";

	/// <summary>
	/// The amount of time to wait if we are in automatic mode before switching labels.
	/// </summary>
	public const float AutoDelay = 3f;

	public enum TextSpeed
	{
		Slow = 105,
		Normal = 70,
		Fast = 30
	}
}

public class Input : IEquatable<InputAction>
{
	[InputAction] public string Action { get; set; } = string.Empty;

	public bool Equals( InputAction? other )
	{
		return Action == other?.Name;
	}

	[Hide, JsonIgnore] public bool Pressed => Sandbox.Input.Pressed( this );

	[Hide, JsonIgnore] public bool Down => Sandbox.Input.Down( this );

	public static implicit operator string( Input input ) => input.Action;
	public static implicit operator Input( string action ) => new() { Action = action };
}
