using System;
using System.Linq;

namespace VNBase.UI;

public partial class DialogueBox
{
	private TextInput? TextInput => Hud?.GetSubPanel<TextInput>();

	private bool CanContinue => Player?.State.IsDialogueFinished == true && !Player.State.Choices.Any() && (TextInput?.CanContinue ?? true);

	protected override int BuildHash()
	{
		return HashCode.Combine( Player?.State.DialogueText, Player?.State.IsDialogueFinished, Player?.State.SpeakingCharacter, Player?.State.Choices.Count, TextInput?.CanContinue );
	}
}
