using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using AvClientMvvmContract.ViewServices;

namespace AvClientView.Services;

/// <summary>
/// Provides the primary monitor's desktop resolution using Avalonia's screen API.
/// Lazy-loaded to avoid querying before the main window is available.
/// </summary>
public class ResolutionService : IResolutionService
{
    private int? _desktopWidth;
    private int? _desktopHeight;

    public int DesktopWidth => _desktopWidth ??= QueryPrimaryScreenBounds().width;
    public int DesktopHeight => _desktopHeight ??= QueryPrimaryScreenBounds().height;

    private static (int width, int height) QueryPrimaryScreenBounds()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow?.Screens is { } screens)
        {
            var primary = screens.Primary;
            if (primary != null)
                return (primary.Bounds.Width, primary.Bounds.Height);

            if (screens.All.Count > 0)
            {
                var first = screens.All[0];
                return (first.Bounds.Width, first.Bounds.Height);
            }
        }

        return (1920, 1080);
    }
}
