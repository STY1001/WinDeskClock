using System.Media;

namespace WinDeskClock.Services
{
    /// <summary>
    /// Plays and stops looping WAV sounds (alarm ringing, timer done). Wraps System.Media.SoundPlayer
    /// so ViewModels don't need to manage SoundPlayer lifetime/disposal themselves.
    /// </summary>
    public interface ISoundService
    {
        /// <summary>Starts looping the given WAV file. Stops any sound already playing on this instance first.</summary>
        void PlayLooping(string wavFilePath);
        void Stop();
    }

    public class SoundService : ISoundService
    {
        private SoundPlayer? _player;

        public void PlayLooping(string wavFilePath)
        {
            Stop();
            try
            {
                _player = new SoundPlayer(wavFilePath);
                _player.Load();
                _player.PlayLooping();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to play sound: " + ex.Message, ex);
            }
        }

        public void Stop()
        {
            _player?.Stop();
            _player?.Dispose();
            _player = null;
        }
    }
}
