using System;
using VNBase.Assets;

namespace VNBase.UI;

public partial class CharacterPortrait
{
	public Character? Character { get; set; }

	protected override int BuildHash()
	{
		return HashCode.Combine( Character, Character?.ActivePortrait, Player?.State?.SpeakingCharacter );
	}
}
