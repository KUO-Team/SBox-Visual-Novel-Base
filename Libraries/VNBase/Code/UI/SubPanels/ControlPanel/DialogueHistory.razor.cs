using System;
using System.Linq;

namespace VNBase.UI;

public partial class DialogueHistory
{
	private bool IsInputPressed => Settings?.HistoryInputs.Any( x => x.Pressed ) ?? false;

	public override void Tick()
	{
		if ( IsInputPressed )
		{
			ToggleVisibility();
		}
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( Player?.DialogueHistory.Count );
	}
}
