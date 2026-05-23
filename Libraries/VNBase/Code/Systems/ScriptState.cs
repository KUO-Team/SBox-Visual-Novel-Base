using Sandbox;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using VNBase.Assets;
using VNScript.State;
using Script = VNScript.Script;

namespace VNBase;

/// <summary>
/// Contains a structure for the current active script state.
/// </summary>
public class ScriptState
{
	/// <summary>
	/// The currently active script label text.
	/// </summary>
	[ReadOnly]
	public string? DialogueText { get; set; }
	
	/// <summary>
	/// Path to the currently active background image.
	/// </summary>
	[ImageAssetPath, ReadOnly]
	public string? Background { get; set; }
	
	/// <summary>
	/// The currently active speaking character.
	/// </summary>
	[ReadOnly]
	public Character? SpeakingCharacter { get; set; }
	
	/// <summary>
	/// Characters to display for this label.
	/// </summary>
	[ReadOnly]
	public List<CharacterState> Characters { get; set; } = [];
	
	/// <summary>
	/// The choices for this dialogue.
	/// </summary>
	[ReadOnly]
	public List<Script.Choice> Choices { get; set; } = [];
	
	[ReadOnly]
	public List<Assets.Sound> Sounds { get; set; } = [];
	
	[JsonIgnore, Hide]
	public MusicPlayer? BackgroundMusic { get; set; }
	
	/// <summary>
	/// If the dialogue has finished writing text.
	/// </summary>
	[ReadOnly]
	public bool IsDialogueFinished { get; set; }
	
	/// <summary>
	/// Clears the active ScriptState.
	/// </summary>
	public void Clear()
	{
		DialogueText = null;
		Background = null;
		SpeakingCharacter = null;
		Characters.Clear();
		Choices.Clear();
		Sounds.Clear();
		BackgroundMusic?.Stop();
		BackgroundMusic = null;
		IsDialogueFinished = false;
	}
	
	public void StopBackgroundMusic()
	{
		BackgroundMusic?.Stop();
	}
}
