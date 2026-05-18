using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.UI;
using VNScript;

namespace VNBase.UI;

public partial class VNHud
{
	[Property, RequireComponent]
	private ScriptPlayer? Player { get; set; }
	
	public DialogueHistory? DialogueHistory { get; private set; }
	
	/// <summary>
	/// Anything in this container div will be centered on the hud
	/// </summary>
	private Panel? _centerContainer;
	
	private Script.Input? Input => Player?.ActiveLabel?.ActiveInput;
	public bool ShouldShowInput => Player?.State.IsDialogueFinished == true && Input is not null;
	
	protected override void OnStart()
	{
		AddCustomStylesheet();
		
		try
		{
			Player = Scene.GetAllComponents<ScriptPlayer>().First();
		}
		catch ( InvalidOperationException )
		{
			ScriptPlayer.Log.Warning( "No ScriptPlayer assigned to VNHud and VNHud could not find a ScriptPlayer in the scene!" );
		}
	}
	
	public T? GetSubPanel<T>() where T : SubPanel
	{
		var subPanels = GetSubPanels();
		return subPanels.OfType<T>().FirstOrDefault();
	}
	
	public List<SubPanel> GetSubPanels()
	{
		List<SubPanel> list = [];
		
		if ( _centerContainer.IsValid() )
		{
			list.AddRange( _centerContainer.ChildrenOfType<SubPanel>() );
		}
		
		list.AddRange( Panel.ChildrenOfType<SubPanel>() );
		
		return list;
	}
	
	private void AddCustomStylesheet()
	{
		var root = Panel.FindRootPanel();
		root.StyleSheet.Load( "/UI/VNHud.razor.scss" );
	}
	
	/// <summary>
	/// Elements to not allow player click passthrough from. <br/>
	/// For example, clicking on a button shouldn't advance the script.
	/// </summary>
	private static readonly Type[] IgnoredAdvancePassthroughElements = [typeof(Button), typeof(DropDown)];
	
	// If the user clicks on the screen, allow advancing the dialogue.
	protected override void OnMouseDown( MousePanelEvent e )
	{
		if ( Player is null )
		{
			return;
		}
		
		// Clicking certain UI elements shouldn't advance.
		if ( IgnoredAdvancePassthroughElements.Contains( e.Target.GetType() ) )
		{
			return;
		}
		
		Player.AdvanceOrSkipDialogueEffect();
	}
	
	protected override int BuildHash()
	{
		return HashCode.Combine( Player, Player?.IsScriptActive, Player?.ActiveScript, Player?.State.GetHashCode() );
	}
}
