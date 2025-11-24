using NativeEngine;

namespace Sandbox.Internal;

public static partial class AudioSettings
{
	public struct AudioDevice
	{
		public string Id { get; private set; }
		public string Name { get; private set; }
		public bool IsAvailable { get; private set; }
		public bool IsDefault { get; private set; }

		internal AudioDevice( string id, string name, AudioDeviceDesc desc )
		{
			Id = id;
			Name = name;
			IsAvailable = desc.IsAvailable;
			IsDefault = desc.IsDefault;
		}
	}

	/// <summary>
	/// Set the active audio device by id
	/// </summary>
	/// <param name="id"></param>
	public static void SetActiveDevice( string id )
	{
		g_pSoundSystem.SetActiveAudioDevice( id );
	}

	/// <summary>
	/// Get the active audio device
	/// </summary>
	/// <returns></returns>
	public static AudioDevice GetActiveDevice()
	{
		var activeId = g_pSoundSystem.GetActiveAudioDevice();
		var devices = GetAudioDevices();
		var device = devices.FirstOrDefault( d => d.Id == activeId );

		if ( device.IsAvailable )
			return device;

		return devices.FirstOrDefault();
	}

	/// <summary>
	/// Get all audio devices supported by the current platform
	/// </summary>
	public static IEnumerable<AudioDevice> GetAudioDevices()
	{
		var deviceCount = g_pSoundSystem.GetNumAudioDevices();

		for ( var i = 0; i < deviceCount; i++ )
		{
			yield return new AudioDevice(
				g_pSoundSystem.GetAudioDeviceId( i ),
				g_pSoundSystem.GetAudioDeviceName( i ),
				g_pSoundSystem.GetAudioDeviceDesc( i )
			);
		}
	}
}
