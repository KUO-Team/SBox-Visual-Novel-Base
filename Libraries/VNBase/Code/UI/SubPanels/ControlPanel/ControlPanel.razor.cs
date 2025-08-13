using System;
using System.Linq;
using Sandbox;
using Sandbox.UI;

namespace VNBase.UI;

public partial class ControlPanel
{
	private Panel? ControlButtons { get; set; }

    private bool UIVisible { get; set; } = true;

    private SubPanel? _activeSubPanel;

    private bool IsAutomaticMode => Player?.IsAutomaticMode ?? false;
    private bool IsInputPressed => Settings?.HideUIInputs.Any( x => x.Pressed ) ?? false;

    public override void Tick()
    {
        if ( IsInputPressed )
        {
            ToggleUI();
        }
    }

    private void Skip()
    {
        Player?.Skip();
    }

    private void ToggleAutomaticMode()
    {
        if ( !Player.IsValid() )
        {
            return;
        }

        Player.IsAutomaticMode = !Player.IsAutomaticMode;
    }

    // ReSharper disable once InconsistentNaming
    private void ToggleUI()
    {
        // Panels that should not be hidden
        var ignoredPanels = new Type[]
        {
            typeof( CharacterPortraits )
        };

        var panelsToHide = Parent.ChildrenOfType<SubPanel>()
            .Where( x => !ignoredPanels.Contains( x.GetType() ) )
            .Skip( 1 );

        foreach ( var panel in panelsToHide )
        {
            panel.ToggleVisibility();
        }

        UIVisible = !UIVisible;
    }

    private void ToggleSubPanel( string id )
    {
        var panel = GetSubPanelFromId( id );

        if ( !panel.IsValid() )
        {
            Log.Warning( $"Unable to find sub panel with id: {id}" );
            return;
        }

        if ( _activeSubPanel.IsValid() && _activeSubPanel != panel )
        {
            _activeSubPanel.Hide();
        }

        _activeSubPanel = panel;
        panel.ToggleVisibility();
    }

    private SubPanel? GetSubPanelFromId( string id )
    {
        return ChildrenOfType<SubPanel>().SingleOrDefault( x => x.ElementName.Equals( id, StringComparison.CurrentCultureIgnoreCase ) );
    }

    protected override void OnAfterTreeRender( bool firstTime )
    {
        if ( !firstTime )
        {
            return;
        }

        if ( !ControlButtons.IsValid() )
        {
            return;
        }

        foreach ( var button in ControlButtons.ChildrenOfType<Button>() )
        {
            if ( button.Id is null )
            {
                continue;
            }

            button.AddEventListener( "onclick", panelEvent => ToggleSubPanel( panelEvent.This.Id ) );
        }
    }

    protected override int BuildHash()
    {
        return HashCode.Combine( UIVisible, _activeSubPanel, Player?.CanSkip(), Player?.IsAutomaticModeAvailable, Player?.State.GetHashCode() );
    }
}
