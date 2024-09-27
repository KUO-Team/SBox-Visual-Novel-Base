﻿using Sandbox.UI;

namespace VNBase.UI;

public class SubPanel : Panel
{
	public ScriptPlayer? Player { get; set; }
	
	public Settings? Settings { get; set; }

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
