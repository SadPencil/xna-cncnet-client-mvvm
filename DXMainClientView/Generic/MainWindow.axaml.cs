using DXMainClientMvvmContract.Generic;
using DXMainClientMvvmContract.Campaign;
using DXMainClientMvvmContract.Multiplayer;
using DXMainClientMvvmContract.Multiplayer.CnCNet;
using DXMainClientMvvmContract.Multiplayer.GameLobby;

using System;

using Avalonia.Controls;
using Avalonia.Threading;


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
        var loadingScreenVM = ViewConstants.ServiceProvider.GetRequiredService<ILoadingScreenViewModel>();
        loadingScreen = new LoadingScreen();
        loadingScreen.ViewModel = loadingScreenVM;

        loadingScreen.Width = 800;
        loadingScreen.Height = 600;

        loadingScreen.Completed += OnLoadingCompleted;

        return loadingScreen;
    }

    private void OnLoadingCompleted(object? sender, EventArgs e)
    {
        ((LoadingScreen)sender!).Completed -= OnLoadingCompleted;
        // TODO: should I fire loadingScreen.Completed in UIThread and therefore remove this Dispatcher call?
        Dispatcher.UIThread.Post(TransitionToMainMenu);
    }

    private MainMenu GetMainMenu()
    {
        var mainMenuVM = ViewConstants.ServiceProvider.GetRequiredService<IMainMenuViewModel>();
        var topBarVM = ViewConstants.ServiceProvider.GetRequiredService<ITopBarViewModel>();
        var campaignSelectorVM = ViewConstants.ServiceProvider.GetRequiredService<ICampaignSelectorViewModel>();
        var optionsWindowVM = ViewConstants.ServiceProvider.GetRequiredService<IOptionsWindowViewModel>();
        var extrasWindowVM = ViewConstants.ServiceProvider.GetRequiredService<IExtrasWindowViewModel>();
        var statisticsWindowVM = ViewConstants.ServiceProvider.GetRequiredService<IStatisticsWindowViewModel>();
        var gameLoadingWindowVM = ViewConstants.ServiceProvider.GetRequiredService<IGameLoadingWindowViewModel>();
        var skirmishLobbyVM = ViewConstants.ServiceProvider.GetRequiredService<ISkirmishLobbyViewModel>();
        var cncNetLobbyVM = ViewConstants.ServiceProvider.GetRequiredService<ICnCNetLobbyViewModel>();
        var lanLobbyVM = ViewConstants.ServiceProvider.GetRequiredService<ILANLobbyViewModel>();
        var privateMessagingVM = ViewConstants.ServiceProvider.GetRequiredService<IPrivateMessagingWindowViewModel>();

        var mainMenu = new MainMenu();
        mainMenu.ViewModel = mainMenuVM;
        mainMenu.SetTopBarViewModel(topBarVM);
        mainMenu.SetCampaignSelectorViewModel(campaignSelectorVM);
        mainMenu.SetOptionsWindowViewModel(optionsWindowVM);
        mainMenu.SetExtrasWindowViewModel(extrasWindowVM);
        mainMenu.SetStatisticsWindowViewModel(statisticsWindowVM);
        mainMenu.SetGameLoadingWindowViewModel(gameLoadingWindowVM);
        mainMenu.SetSkirmishLobbyViewModel(skirmishLobbyVM);
        mainMenu.SetCnCNetLobbyViewModel(cncNetLobbyVM);
        mainMenu.SetLANLobbyViewModel(lanLobbyVM);
        mainMenu.SetPrivateMessagingWindowViewModel(privateMessagingVM);

        return mainMenu;
    }

    private void TransitionToMainMenu()
    {
        loadingScreen = null;
        MainContent.Content = mainMenu;
    }
}
