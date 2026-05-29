using System.ComponentModel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DXMainClientView.Generic;
using DXMainClientViewModel.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView;

public class App : Application
{
    internal static ServiceProvider? ServiceProvider { get; set; }

    private LoadingScreen? _loadingScreen;
    private MainMenu? _mainMenu;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var loadingScreenVM = ServiceProvider!.GetRequiredService<ILoadingScreenViewModel>();

            _loadingScreen = new LoadingScreen();
            _loadingScreen.ViewModel = loadingScreenVM;

            // Watch for loading completion
            loadingScreenVM.PropertyChanged += OnLoadingScreenPropertyChanged;

            desktop.MainWindow = _loadingScreen;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnLoadingScreenPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ILoadingScreenViewModel.IsLoading))
        {
            var loadingVM = (ILoadingScreenViewModel)sender!;
            if (!loadingVM.IsLoading)
            {
                // Loading complete - transition to MainMenu
                Dispatcher.UIThread.Post(ShowMainMenu);
            }
        }
    }

    private void ShowMainMenu()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        // Unsubscribe from loading screen
        if (_loadingScreen?.ViewModel != null)
            _loadingScreen.ViewModel.PropertyChanged -= OnLoadingScreenPropertyChanged;

        // Resolve MainMenu ViewModels from DI
        var mainMenuVM = ServiceProvider!.GetRequiredService<IMainMenuViewModel>();
        var topBarVM = ServiceProvider!.GetRequiredService<ITopBarViewModel>();

        // Create MainMenu view and wire up ViewModels
        _mainMenu = new MainMenu();
        _mainMenu.ViewModel = mainMenuVM;
        _mainMenu.SetTopBarViewModel(topBarVM);

        // Transition windows
        desktop.MainWindow = _mainMenu;
        _mainMenu.Show();
        _loadingScreen?.Close();
        _loadingScreen = null;
    }
}
