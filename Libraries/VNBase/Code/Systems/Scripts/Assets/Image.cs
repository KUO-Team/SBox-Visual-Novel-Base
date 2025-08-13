using Sandbox;

namespace VNBase.Assets;

public class Image( string path ) : IAsset
{
	[FilePath]
	public string Path { get; set; } = path;
}
