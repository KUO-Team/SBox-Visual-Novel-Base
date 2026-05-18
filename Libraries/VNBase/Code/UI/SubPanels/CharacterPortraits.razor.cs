using System;

namespace VNBase.UI;

public partial class CharacterPortraits
{
	private bool HasCharacters => Player?.State.Characters.Count != 0;
	
	protected override int BuildHash()
	{
		return HashCode.Combine( Player?.State.DialogueText, Player?.State.Characters.Count, HasCharacters );
	}
}
