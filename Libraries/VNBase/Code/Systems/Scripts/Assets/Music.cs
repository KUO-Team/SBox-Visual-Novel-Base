namespace VNBase.Assets;

/// <summary>
/// Represents a music asset.
/// </summary>
public class Music( string path ) : IAsset
{
	public string Path { get; set; } = path;
	
	/// <summary>
	/// The name of the target mixer.
	/// </summary>
	public string MixerName { get; set; } = "Music";
	
	public Music( string path, string mixerName ) : this( path )
	{
		MixerName = mixerName;
	}
}
