using System;
using Avalonia.Controls;
using Avalonia.Threading;
using DXMainClientViewModel.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView.Generic;

public partial class MainWindow : Window
{
    private LoadingScreen? loadingScreen;
    private MainMenu? mainMenu;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void ShowLoadingScreen(ILoadingScreenViewModel loadingScreenVM)
    {
        loadingScreen = new LoadingScreen();
        loadingScreen.ViewModel = loadingScreenVM;
        loadingScreen.Completed += OnLoadingCompleted;
        MainContent.Content = loadingScreen;
    }

    private void OnLoadingCompleted(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(TransitionToMainMenu);
    }

    private void TransitionToMainMenu()
    {
        if (loadingScreen != null)
        {
            loadingScreen.Completed -= OnLoadingCompleted;
            loadingScreen = null;
        }

        var provider = App.ServiceProvider;
        if (provider == null)
            return;

        var mainMenuVM = provider.GetRequiredService<IMainMenuViewModel>();
        var topBarVM = provider.GetRequiredService<ITopBarViewModel>();

        mainMenu = new MainMenu();
        mainMenu.ViewModel = mainMenuVM;
        mainMenu.SetTopBarViewModel(topBarVM);
        MainContent.Content = mainMenu;
        // MainMenu applies its own INI layout in its Loaded handler
    }
}
