using Sandbox;
using Sandbox.Audio;

namespace VNBase.Assets;

/// <summary>
/// A playable sound asset.
/// </summary>
public class Sound( string eventName ) : IAsset
{
	/// <summary>
	/// The name of the <see cref="SoundEvent"/> this asset is tied to.
	/// </summary>
	private string EventName { get; set; } = eventName;

	/// <summary>
	/// If this asset is constructed with a SoundEvent, this is that event. Otherwise, null.
	/// </summary>
	public SoundEvent? Event { get; private set; }

	/// <summary>
	/// Handle to interface with the playing sound. If this asset isn't playing, this is null.
	/// </summary>
	public SoundHandle? Handle { get; private set; }
	
	/// <summary>
	/// The name of the target mixer.
	/// </summary>
	public string MixerName { get; set; } = string.Empty;

	/// <summary>
	/// If this asset is constructed with a SoundEvent, returns the path to the event on disk.
	/// Otherwise, returns an empty string.
	/// </summary>
	[FilePath]
	public string Path
	{
		get
		{
			return Event is not null ? Event.ResourcePath : _path;
		}
		set
		{
			_path = value;
		}
	}

	private string _path = string.Empty;

	public SoundHandle Play()
	{
		Handle = Sandbox.Sound.Play( EventName );
		return Handle;
	}

	public SoundHandle Play( string mixerName )
	{
		var handle = Play();

		if ( Mixer.FindMixerByName( mixerName ) is not {} mixer )
		{
			Log.Warning( $"No mixer found with the name: {mixerName}." );
			return handle;
		}
		
		handle.TargetMixer = mixer;
		MixerName = handle.TargetMixer.Name;

		return handle;
	}

	public void Stop( float fadeTime = 0 )
	{
		Handle?.Stop( fadeTime );
	}
}
