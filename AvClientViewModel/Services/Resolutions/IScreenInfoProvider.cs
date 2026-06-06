using System.Collections.Generic;

namespace AvClientViewModel.Services.Resolutions;

/// <summary>
/// Provides the primary monitor's desktop resolution and supported display modes.
/// Each platform has its own implementation using OS-level APIs.
/// </summary>
internal interface IScreenInfoProvider
{
    /// <summary>
    /// Whether this provider can run on the current system.
    /// Providers check that required OS APIs or commands are available.
    /// Called in parallel across all providers; keep it fast.
    /// </summary>
    bool IsApplicable();

    /// <summary>
    /// Priority when selecting among applicable providers. Higher = tried first.
    /// Platform providers default to 0; the dummy fallback uses int.MinValue.
    /// </summary>
    int Priority { get; }

    int DesktopWidth { get; }
    int DesktopHeight { get; }
    IReadOnlyList<(int Width, int Height)> GetSupportedDisplayModes();
}
