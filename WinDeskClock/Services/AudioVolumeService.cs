using NAudio.CoreAudioApi;

namespace WinDeskClock.Services
{
    /// <summary>
    /// Reads/writes the system's default playback device volume (the "Volume" slider in
    /// Settings > General controls the OS volume, not an app-internal one). Wraps NAudio so
    /// ViewModels don't depend on NAudio.CoreAudioApi directly.
    /// </summary>
    public interface IAudioVolumeService
    {
        /// <summary>Current system volume, 0-100.</summary>
        int GetVolumePercent();

        /// <summary>Sets the system volume, 0-100.</summary>
        void SetVolumePercent(int percent);
    }

    public class AudioVolumeService : IAudioVolumeService
    {
        private static MMDevice GetDefaultPlaybackDevice()
        {
            using var enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        }

        public int GetVolumePercent()
        {
            using var device = GetDefaultPlaybackDevice();
            return (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
        }

        public void SetVolumePercent(int percent)
        {
            using var device = GetDefaultPlaybackDevice();
            device.AudioEndpointVolume.MasterVolumeLevelScalar = percent / 100f;
        }
    }
}
