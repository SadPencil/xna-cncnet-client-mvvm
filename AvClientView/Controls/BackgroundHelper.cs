using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using AvClientView.Services;

namespace AvClientView.Controls;

/// <summary>
/// Shared helper for applying default background textures to controls,
/// eliminating code duplication across 14 view code-behind files.
/// Background is not directly accessible on the abstract Control type in
/// the current Avalonia version, so we provide concrete-type overloads.
/// </summary>
public static class BackgroundHelper
{
    public static void ApplyDefaultBackground(UserControl control, string texturePath, IIniLayoutOverlayService? iniOverlay)
    {
        try
        {
            if (iniOverlay == null) return;
            var fullPath = iniOverlay.FindTextureFile(texturePath);
            if (fullPath != null)
            {
                var bitmap = new Bitmap(fullPath);
                control.Background = new ImageBrush { Source = bitmap, Stretch = Stretch.UniformToFill };
            }
        }
        catch { }
    }

    public static void ApplyDefaultBackground(Panel panel, string texturePath, IIniLayoutOverlayService? iniOverlay)
    {
        try
        {
            if (iniOverlay == null) return;
            var fullPath = iniOverlay.FindTextureFile(texturePath);
            if (fullPath != null)
            {
                var bitmap = new Bitmap(fullPath);
                panel.Background = new ImageBrush { Source = bitmap, Stretch = Stretch.UniformToFill };
            }
        }
        catch { }
    }
}
