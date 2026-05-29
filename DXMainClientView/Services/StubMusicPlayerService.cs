using System;
using DXMainClientViewModel;

namespace DXMainClientView.Services;

/// <summary>
/// Stub implementation of IMusicPlayerService for the View layer.
/// No music playback in Avalonia view-only mode.
/// </summary>
public class StubMusicPlayerService : IMusicPlayerService
{
    public bool IsAvailable => false;
    public bool IsPlaying => false;
    public float Volume { get; set; }
    public bool IsRepeating { get; set; }
    public bool IsFading => false;

    public void PlayThemeSong() { }
    public void Stop() { }
    public void StartFadeOut(float fadeStepPerSecond, Action onComplete) { }
    public void StartExitFade(float fadeStep, Action onComplete) { }
    public void UpdateFade(float elapsedSeconds) { }
    public void Dispose() { }
}
