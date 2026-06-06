using System.Collections.Generic;

namespace AvClientViewModel.Services;

/// <summary>
/// Provides the primary monitor's desktop resolution and supported display modes.
/// Each platform has its own implementation using OS-level APIs.
/// </summary>
internal interface IScreenInfoProvider
{
    /// <summary>
    /// Whether this provider can run on the current system.
    /// Providers check that required OS APIs or commands are available.
    /// </summary>
    bool IsApplicable { get; }

    int DesktopWidth { get; }
    int DesktopHeight { get; }
    IReadOnlyList<(int Width, int Height)> GetSupportedDisplayModes();
}
