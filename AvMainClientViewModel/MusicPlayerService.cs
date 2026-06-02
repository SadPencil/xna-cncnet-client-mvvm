using System;

using Rampastring.Tools;

namespace AvMainClientViewModel
{
    /// <summary>
    /// Implementation of IMusicPlayerService for non-XNA platforms.
    /// Reports IsAvailable = false when XNA MediaPlayer is not available.
    /// The View layer can provide a platform-specific implementation if needed.
    /// </summary>
    public class MusicPlayerService : IMusicPlayerService
    {
        public bool IsAvailable => false;

        public bool IsPlaying => false;

        public float Volume { get; set; }

        public bool IsRepeating { get; set; }

        public bool IsFading => false;

        public void PlayThemeSong()
        {
            Logger.Log("MusicPlayerService: Music playback not available on this platform.");
        }

        public void Stop() { }

        public void StartFadeOut(float fadeStepPerSecond, Action onComplete)
        {
            onComplete?.Invoke();
        }

        public void StartExitFade(float fadeStep, Action onComplete)
        {
            onComplete?.Invoke();
        }

        public void UpdateFade(float elapsedSeconds) { }

        public void Dispose() { }
    }
}
