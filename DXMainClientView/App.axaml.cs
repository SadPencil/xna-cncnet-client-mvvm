using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DXMainClientView.Generic;
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
            desktop.MainWindow = loadingScreen;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
