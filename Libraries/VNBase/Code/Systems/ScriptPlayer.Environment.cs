using SandLang;

namespace VNBase;

public sealed partial class ScriptPlayer
{
	/// <summary>
	/// The active dialogue environment.
	/// Will be empty if there is no active dialogue.
	/// </summary>
	private IEnvironment _environment = new EnvironmentMap();

	protected override void OnDestroy()
	{
		GlobalEnvironment.Clear();
		base.OnDestroy();
	}

	/// <summary>
	/// Sets the active dialogue environment.
	/// </summary>
	private void SetEnvironment( Dialogue dialogue )
	{
		if ( ActiveScript is null )
		{
			Log.Error( "No active script to set environment for!" );
			return;
		}

		var environment = ActiveScript.GetEnvironment();

		foreach ( var variable in dialogue.Variables )
		{
			var variableName = ((Value.VariableReferenceValue)variable.Key).Name;

			if ( variable.Value is Value.VariableReferenceValue reference )
			{
				environment.SetVariable( variableName, reference.Evaluate( environment ) );
			}
			else
			{
				environment.SetVariable( variableName, variable.Value );
			}
		}

		foreach ( var label in dialogue.Labels.Values )
		{
			label.Environment = environment;
		}

		_environment = environment;
	}
}
