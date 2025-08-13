using Sandbox.UI;

namespace VNBase.UI;

public class SubPanel : Panel
{
	[Parameter]
	public ScriptPlayer? Player { get; set; }
	
	[Parameter]
	public Settings? Settings { get; set; }

	[Parameter]
	public VNHud? Hud { get; set; }

	public void ToggleVisibility()
	{
		SetClass( "hidden", IsVisible );
	}

	public void Hide()
	{
		AddClass( "hidden" );
	}

	public void Show()
	{
		RemoveClass( "hidden" );
	}
}
