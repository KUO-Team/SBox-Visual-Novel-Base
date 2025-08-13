using SandLang;
using System.Collections.Generic;

namespace VNBase;

public sealed partial class ScriptPlayer
{
	/// <summary>
	/// All previously shown dialogue entries
	/// </summary>
	public List<HistoryEntry> DialogueHistory { get; } = [];
    
	/// <summary>
	/// Adds a dialogue entry to history
	/// </summary>
	private void AddToHistory( string displayedText, Dialogue.Label label )
	{
		var historyEntry = new HistoryEntry
		{
			Text = displayedText,
			Label = label
		};

		DialogueHistory.Add( historyEntry );
	}
	
	/// <summary>
	/// Represents a single dialogue entry that was actually displayed to the player
	/// </summary>
	public readonly struct HistoryEntry( string text, Dialogue.Label label )
	{
		public string Text { get; init; } = text;
		public Dialogue.Label Label { get; init; } = label;
	}
}
