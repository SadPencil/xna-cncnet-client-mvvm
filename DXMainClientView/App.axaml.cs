using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DXMainClientView.Generic;
using DXMainClientView.Services;
using DXMainClientViewModel.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView;

public class App : Application
{
    /// <summary>
    /// Service provider set by Program.cs before the app starts.
    /// </summary>
    internal static ServiceProvider? ServiceProvider { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var loadingScreenVM = ServiceProvider!.GetRequiredService<ILoadingScreenViewModel>();
            var loadingScreen = new LoadingScreen();
            loadingScreen.ViewModel = loadingScreenVM;

            // Apply INI layout overrides (reads LoadingScreen.ini, GenericWindow.ini)
            var iniOverlay = ServiceProvider.GetService<IIniLayoutOverlayService>();
            iniOverlay?.ApplyLayout(loadingScreen, "LoadingScreen");

            desktop.MainWindow = loadingScreen;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
