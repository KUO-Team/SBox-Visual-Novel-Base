using Sandbox;
using System.Text.Json.Serialization;

namespace VNBase.Assets;

/// <summary>
/// Defines a VNBase character.
/// </summary>
[AssetType( Name = "Character", Extension = "char", Category = "VNBase" )]
public sealed class Character : AssetResource
{
	/// <summary>
	/// The name of the character.
	/// </summary>
	public string Name { get; set; } = string.Empty;
	
	/// <summary>
	/// The title of the character.
	/// If blank, we assume no title.
	/// </summary>
	public string? Title { get; set; }
	
	/// <summary>
	/// The color of the character's name.
	/// </summary>
	public Color NameColor { get; set; } = Color.White;
	
	/// <summary>
	/// The color of the character's title.
	/// </summary>
	public Color TitleColor { get; set; } = Color.White;
}
