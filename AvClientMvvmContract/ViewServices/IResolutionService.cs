namespace AvClientMvvmContract.ViewServices;

/// <summary>
/// Provides the primary monitor's desktop resolution.
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
}
