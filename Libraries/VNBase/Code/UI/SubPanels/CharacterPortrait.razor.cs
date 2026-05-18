using System;
using Sandbox.UI;
using VNScript.State;

namespace VNBase.UI;

public partial class CharacterPortrait
{
	[Parameter]
	public CharacterState? Character { get; set; }
	
	[Parameter]
	public Vector2 Position
	{
		get;
		set
		{
			var oldValue = field;
			
			// If we are manually changing the position, we want to do absolute positioning so
			// that the position actually changes.
			if ( value != oldValue )
			{
				Style.Position = PositionMode.Absolute;
			}
			else
			{
				Style.Position = PositionMode.Static;
			}
			
			field = value;
			Style.Left = Length.Pixels( value.x );
			Style.Top = Length.Pixels( value.y );
		}
	}
	
	[Parameter]
	public Angles Rotation
	{
		get;
		set
		{
			field = value;
			
			var transform = new PanelTransform();
			transform.AddRotation( value.AsVector3() );
			Style.Transform = transform;
		}
	}
	
	protected override int BuildHash()
	{
		return HashCode.Combine( Character, Character?.ActivePortrait, Player?.State.SpeakingCharacter );
	}
}
