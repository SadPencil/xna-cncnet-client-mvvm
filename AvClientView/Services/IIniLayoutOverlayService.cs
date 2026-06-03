using Avalonia.Controls;

namespace AvClientView.Services;

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
    /// <param name="effectiveWidth">Optional override for the effective parent width used by
    /// DistanceFromRightBorder, FillWidth, etc. When null (default), walks up the visual tree.</param>
    /// <param name="effectiveHeight">Optional override for the effective parent height.</param>
    void ApplyLayout(Control control, string sectionName, double? effectiveWidth = null, double? effectiveHeight = null);

    /// <summary>
    /// Finds a texture file in the resource paths (theme first, then base).
    /// Returns the full path, or null if not found.
    /// </summary>
    string FindTextureFile(string texturePath);

    /// <summary>
    /// Applies standard button textures and styling to a control and all its
    /// Button descendants (without the full ApplyLayout processing that adds
    /// ExtraControls / window chrome).
    /// </summary>
    void ApplyStandardButtonStyling(Control control);
}
