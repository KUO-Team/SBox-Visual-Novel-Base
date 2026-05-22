using VNBase.UI;

namespace VNBase;

public sealed partial class ScriptPlayer
{
	private bool _isSkipping;
	
	public void Skip()
	{
		if ( ActiveScript is null || ActiveLabel is null )
		{
			return;
		}
		
		if ( !CanSkip() )
		{
			return;
		}
		
		_isSkipping = true;
		var scriptUnloaded = false;
		
		try
		{
			var currentLabel = ActiveLabel;
			
			while ( currentLabel?.AfterLabel is not null )
			{
				var scriptBeforeAdvance = ActiveScript;
				var labelBeforeAdvance = ActiveLabel;
				ExecuteAfterLabel();
				SkipDialogueEffect();
				currentLabel = ActiveLabel;
				
				if ( ActiveScript is null || currentLabel is null )
				{
					scriptUnloaded = true;
					return;
				}
				
				if ( !ReferenceEquals( ActiveScript, scriptBeforeAdvance ) )
				{
					return;
				}
				
				if ( ReferenceEquals( currentLabel, labelBeforeAdvance ) )
				{
					break;
				}
				
				// Check if we hit input.
				if ( currentLabel.ActiveInput is not null )
				{
					return;
				}
				
				// Check if we hit a choice.
				if ( currentLabel.Choices.Count > 0 )
				{
					return;
				}
				
				if ( currentLabel.AfterLabel is null || !currentLabel.AfterLabel.IsLastLabel )
				{
					continue;
				}
				
				scriptUnloaded = true;
				UnloadScript();
				
				return;
			}
		}
		finally
		{
			_isSkipping = false;
			
			if ( !scriptUnloaded && IsScriptActive && ActiveLabel is not null )
			{
				_ = DisplayCurrentTextSegment();
			}
		}
	}
	
	/// <summary>
	/// Checks if the current dialogue section can be skipped.
	/// </summary>
	public bool CanSkip()
	{
		if ( ActiveScript is null || ActiveLabel is null )
		{
			return false;
		}
		
		// TODO: Automatic mode skipping is broken. Investigate.
		// For now we just don't allow skipping if we are in automatic mode.
		if ( IsAutomaticMode )
		{
			return false;
		}
		
		var hasTextInput = Hud?.GetSubPanel<TextInput>() is not null;
		
		if ( hasTextInput )
		{
			return false;
		}
		
		var hasChoices = ActiveLabel.Choices.Count > 0;
		
		return !hasChoices;
	}
}
