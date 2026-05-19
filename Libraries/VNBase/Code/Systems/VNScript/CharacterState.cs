using Sandbox;
using VNBase;
using VNBase.Assets;

namespace VNScript.State;

public sealed class CharacterState
{
	public required Character Character { get; init; }
	
	/// <summary>
	/// The name of the active portrait image.
	/// Includes extension.
	/// </summary>
	public string? ActivePortrait { get; set; }
	
	/// <summary>
	/// Path to the active portrait image.
	/// </summary>
	[FilePath]
	public string ActivePortraitPath => $"{Settings.CharacterPortraitsPath}/{Character.Name}/{ActivePortrait}";
	
	public bool HasManualPosition { get; set; }
	
	public Vector2 Position { get; set; }
	
	public Angles Rotation { get; set; }
}
