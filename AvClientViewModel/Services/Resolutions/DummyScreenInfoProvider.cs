using System.Collections.Generic;

namespace AvClientViewModel.Services.Resolutions;

/// <summary>
/// Fallback provider when no platform-specific screen query is available.
/// Returns a single 1280x720 mode.
/// </summary>
internal sealed class DummyScreenInfoProvider : IScreenInfoProvider
{
    public int Priority => int.MinValue;
    public bool IsApplicable => true;

    public int DesktopWidth => 1280;
    public int DesktopHeight => 720;

    public IReadOnlyList<(int Width, int Height)> GetSupportedDisplayModes() => [(1280, 720)];
}
