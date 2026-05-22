namespace VNBase.Assets;

/// <summary>
/// Represents a music asset.
/// </summary>
public class Music( string path ) : IAsset
{
	public string Path { get; set; } = path;
}
