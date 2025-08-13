using System;
using Sandbox;
using Sandbox.UI;
using SandLang;

namespace VNBase.UI;

public partial class TextInput
{
	public TextEntry? Entry { get; private set; }
	public bool CanContinue => !string.IsNullOrWhiteSpace( Entry?.Text );
	private Dialogue.Input? Input => Player?.ActiveLabel?.ActiveInput;

	private void Submit()
	{
		if ( Input is null || !Entry.IsValid() )
		{
			return;
		}

		var environment = Player?.ActiveScript?.GetEnvironment();
		if ( environment is null )
		{
			Log.Warning( "Unable to submit value; no active environment found!" );
			return;
		}

		var value = new Value.StringValue( Entry.Text );
		Input.SetValue( environment, value );
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( Player?.State.IsDialogueFinished, Player?.ActiveScript, Player?.ActiveLabel );
	}
}
