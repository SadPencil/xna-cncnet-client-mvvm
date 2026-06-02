using System;

namespace AvMainClientViewModel
{
    /// <summary>
    /// Service interface for music playback.
    /// Abstracts Microsoft.Xna.Framework.Media.MediaPlayer.
    /// </summary>
    public interface IMusicPlayerService
    {
        /// <summary>
        /// Whether the media player is available on the current system.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Whether music is currently playing.
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Current playback volume (0.0 to 1.0).
        /// </summary>
        float Volume { get; set; }

        /// <summary>
        /// Whether the player should repeat.
        /// </summary>
        bool IsRepeating { get; set; }

        /// <summary>
        /// Loads and starts playing the theme song.
        /// </summary>
        void PlayThemeSong();

        /// <summary>
        /// Stops music playback.
        /// </summary>
        void Stop();

        /// <summary>
        /// Starts fading the music out. Calls onComplete when fade finishes.
        /// </summary>
        void StartFadeOut(float fadeStepPerSecond, Action onComplete);

        /// <summary>
        /// Starts a quick fade-out for exiting. Calls onComplete when fade finishes.
        /// </summary>
        void StartExitFade(float fadeStep, Action onComplete);

        /// <summary>
        /// Updates the fade animation. Called each frame by the View.
        /// </summary>
        void UpdateFade(float elapsedSeconds);

        /// <summary>
        /// Whether a fade-out is currently in progress.
        /// </summary>
        bool IsFading { get; }

        /// <summary>
        /// Disposes resources (theme song).
        /// </summary>
        void Dispose();
    }
}
