using Sandbox;
using System;
using System.Collections.Generic;
using VNBase.Assets;
using SandLang;

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
	public List<Character> Characters { get; set; } = [];

	/// <summary>
	/// The choices for this dialogue.
	/// </summary>
	[ReadOnly]
	public List<Dialogue.Choice> Choices { get; set; } = [];
	
	/// <summary>
	/// Any currently playing sounds.
	/// </summary>
	[ReadOnly]
	public List<Assets.Sound> Sounds { get; set; } = [];
	
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
		SpeakingCharacter = null;
		Background = null;
		IsDialogueFinished = false;
		Characters.Clear();
		Choices.Clear();
	}

	public override int GetHashCode()
	{
		// ReSharper disable NonReadonlyMemberInGetHashCode
		return HashCode.Combine( DialogueText, Background, SpeakingCharacter, Characters.Count, Choices.Count );
	}
}
