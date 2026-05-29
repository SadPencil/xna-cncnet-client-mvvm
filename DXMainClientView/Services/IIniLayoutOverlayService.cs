using Avalonia.Controls;

namespace DXMainClientView.Services;

/// <summary>
/// Applies INI-based layout overrides to Avalonia controls.
/// Reads INI files and sets position, size, visibility, textures, etc.
/// Each view should set its own defaults (background, etc.) before calling
/// ApplyLayout, matching the original XNA pattern where each XNAWindow
/// subclass sets defaults in Initialize() before base.Initialize() reads INI.
/// </summary>
public interface IIniLayoutOverlayService
{
    /// <summary>
    /// Applies INI layout overrides to a control and all its named descendants.
    /// The control itself is treated as the root (e.g., Window or UserControl)
    /// and gets properties from the named INI section.
    /// </summary>
    /// <param name="control">The root control to apply layout to.</param>
    /// <param name="sectionName">The INI section name (e.g., "MainMenu", "LoadingScreen").</param>
    void ApplyLayout(Control control, string sectionName);

    /// <summary>
    /// Finds a texture file in the resource paths (theme first, then base).
    /// Returns the full path, or null if not found.
    /// </summary>
    string FindTextureFile(string texturePath);
}
