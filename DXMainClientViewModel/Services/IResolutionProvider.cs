using System.Collections.Generic;

namespace DXMainClientViewModel.Services;

/// <summary>
/// Provides available screen resolutions for display options.
/// </summary>
public interface IResolutionProvider
{
    /// <summary>
    /// Gets available in-game resolutions as strings (e.g., "1920x1080").
    /// </summary>
    IReadOnlyList<string> GetIngameResolutions();

    /// <summary>
    /// Gets available client resolutions as strings (e.g., "1920x1080").
    /// </summary>
    IReadOnlyList<string> GetClientResolutions();

    /// <summary>
    /// Gets the safe full-screen resolution string (e.g., "1920x1080").
    /// </summary>
    string GetSafeFullScreenResolution();

    /// <summary>
    /// Gets the best recommended resolution string for windowed mode.
    /// </summary>
    string GetBestRecommendedResolution();
}
