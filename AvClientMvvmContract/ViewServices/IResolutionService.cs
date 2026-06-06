using System.Collections.Generic;

namespace AvClientMvvmContract.ViewServices;

/// <summary>
/// Provides primary monitor display information.
/// Implemented by the View layer since it has access to platform screen APIs.
/// </summary>
public interface IResolutionService
{
    /// <summary>
    /// Gets the primary monitor's current width in pixels.
    /// </summary>
    int DesktopWidth { get; }

    /// <summary>
    /// Gets the primary monitor's current height in pixels.
    /// </summary>
    int DesktopHeight { get; }

    /// <summary>
    /// Gets all display modes supported by the primary monitor.
    /// Replaces the XNA GraphicsAdapter.DefaultAdapter.SupportedDisplayModes query.
    /// </summary>
    IReadOnlyList<(int Width, int Height)> GetSupportedDisplayModes();
}
