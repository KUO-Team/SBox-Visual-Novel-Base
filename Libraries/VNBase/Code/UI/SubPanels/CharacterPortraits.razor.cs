using System;

namespace VNBase.UI;

public partial class CharacterPortraits
{
	protected override int BuildHash()
	{
		return HashCode.Combine( Player?.State.DialogueText, Player?.State.Characters.Count );
	}
}
