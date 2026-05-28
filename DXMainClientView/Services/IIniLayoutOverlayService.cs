using Avalonia.Controls;

namespace DXMainClientView.Services;

/// <summary>
/// Applies INI-based layout overrides to Avalonia controls.
/// Reads INI files and sets position, size, visibility, textures, etc.
/// </summary>
public interface IIniLayoutOverlayService
{
    /// <summary>
    /// Applies INI layout overrides to a window and all its named children.
    /// </summary>
    /// <param name="window">The window to apply layout to.</param>
    /// <param name="windowName">The INI section name for the window (e.g., "LoadingScreen").</param>
    void ApplyLayout(Window window, string windowName);
}
