using System;

using Avalonia.Controls;
using Avalonia.Threading;

using DXMainClientViewModel.Campaign;
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

    public void ShowMainWindow()
    {
        loadingScreen = GetLoadingScreen();
        MainContent.Content = loadingScreen;

        mainMenu = GetMainMenu();
    }

    private LoadingScreen GetLoadingScreen()
    {
        var loadingScreenVM = App.ServiceProvider!.GetRequiredService<ILoadingScreenViewModel>();
        loadingScreen = new LoadingScreen();
        loadingScreen.Completed += OnLoadingCompleted; // Subscribe before setting DataContext to ensure we catch completion events
        loadingScreen.ViewModel = loadingScreenVM;

        // Set design resolution for ViewBox scaling (INI overlay may override later)
        loadingScreen.Width = 800;
        loadingScreen.Height = 600;

        return loadingScreen;
    }

    private void OnLoadingCompleted(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(TransitionToMainMenu);
    }

    private MainMenu GetMainMenu()
    {
        var mainMenuVM = App.ServiceProvider!.GetRequiredService<IMainMenuViewModel>();
        var topBarVM = App.ServiceProvider!.GetRequiredService<ITopBarViewModel>();
        var campaignSelectorVM = App.ServiceProvider!.GetRequiredService<ICampaignSelectorViewModel>();
        var optionsWindowVM = App.ServiceProvider!.GetRequiredService<IOptionsWindowViewModel>();
        var extrasWindowVM = App.ServiceProvider!.GetRequiredService<IExtrasWindowViewModel>();
        var gameLoadingWindowVM = App.ServiceProvider!.GetRequiredService<IGameLoadingWindowViewModel>();

        var mainMenu = new MainMenu();
        mainMenu.ViewModel = mainMenuVM;
        mainMenu.SetTopBarViewModel(topBarVM);
        mainMenu.SetCampaignSelectorViewModel(campaignSelectorVM);
        mainMenu.SetOptionsWindowViewModel(optionsWindowVM);
        mainMenu.SetExtrasWindowViewModel(extrasWindowVM);
        mainMenu.SetGameLoadingWindowViewModel(gameLoadingWindowVM);

        return mainMenu;
    }

    private void TransitionToMainMenu()
    {
        if (loadingScreen != null)
        {
            loadingScreen.Completed -= OnLoadingCompleted;
            loadingScreen = null;
        }

        MainContent.Content = mainMenu;
        // MainMenu applies its own INI layout in its Loaded handler
    }
}
