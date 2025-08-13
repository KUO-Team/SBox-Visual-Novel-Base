using System;
using SandLang;

namespace VNBase.UI;

public partial class Choices
{
	private void ExecuteChoice( Dialogue.Choice choice )
	{
		Player?.ExecuteChoice( choice );
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( Player?.State.DialogueText, Player?.State.IsDialogueFinished, Player?.State.Choices.Count );
	}
}
