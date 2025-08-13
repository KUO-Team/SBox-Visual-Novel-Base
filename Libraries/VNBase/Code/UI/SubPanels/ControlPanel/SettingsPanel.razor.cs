using System;
using System.Linq;
using Sandbox;
using Sandbox.Audio;
using Sandbox.UI;

namespace VNBase.UI;

public partial class SettingsPanel
{
	private bool IsInputPressed => Settings?.SettingsInputs.Any( x => x.Pressed ) ?? false;
	
	private static Mixer MasterMixer => Mixer.Master;
	
	private static Mixer[] Mixers => Mixer.Master.GetChildren();
	
	private DropDown? _speedDropdown;

	public override void Tick()
	{
		if ( IsInputPressed )
		{
			ToggleVisibility();
		}
	}

	private bool IsDirty()
	{
		var masterVolume = MasterMixer.Volume;

		if ( !Settings.IsValid() )
		{
			return false;
		}

		var textSpeed = Settings.TextEffectSpeed;
		var anyMixerVolumeNotOne = Mixers.Any( mixer => mixer.Volume != 1f );

		return masterVolume != 1f || anyMixerVolumeNotOne || textSpeed != Settings.TextSpeed.Normal;
	}

	private void Reset()
	{
		MasterMixer.Volume = 1f;

		foreach ( var mixer in Mixers )
		{
			mixer.Volume = 1f;
		}

		if ( !Settings.IsValid() )
		{
			return;
		}

		Settings.TextEffectSpeed = Settings.TextSpeed.Normal;

		if ( _speedDropdown.IsValid() )
		{
			_speedDropdown.Value = Enum.GetName( Settings.TextEffectSpeed );
		}
	}

	private void OnFilterChanged()
	{
		if ( !Settings.IsValid() )
		{
			return;
		}

		var stringValue = (string?)_speedDropdown?.Value;
		if ( stringValue is not null )
		{
			Settings.TextEffectSpeed = Enum.Parse<Settings.TextSpeed>( stringValue );
		}
	}

	protected override void OnAfterTreeRender( bool firstTime )
	{
		if ( !firstTime )
		{
			return;
		}

		if ( !_speedDropdown.IsValid() )
		{
			return;
		}

		if ( !Settings.IsValid() )
		{
			return;
		}

		_speedDropdown.Value = Enum.GetName( Settings.TextEffectSpeed );
		base.OnAfterTreeRender( firstTime );
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( Player?.DialogueHistory.Count, Settings?.TextEffectSpeed );
	}
}
