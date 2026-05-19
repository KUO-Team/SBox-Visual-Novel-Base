using System;
using Sandbox.UI;
using VNScript.State;

namespace VNBase.UI;

public partial class CharacterPortrait
{
	[Parameter]
	public CharacterState? Character { get; set; }
	
	[Parameter]
	public bool HasManualPosition { get; set; }
	
	[Parameter]
	public Vector2 Position
	{
		get;
		set
		{
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
	
	protected override void OnParametersSet()
	{
		Style.Position = HasManualPosition
			? PositionMode.Absolute
			: PositionMode.Static;
		
		base.OnParametersSet();
	}
	
	protected override int BuildHash()
	{
		return HashCode.Combine( Character, Character?.ActivePortrait, Character?.Position, Character?.HasManualPosition, Character?.Rotation, Player?.State.SpeakingCharacter );
	}
}
