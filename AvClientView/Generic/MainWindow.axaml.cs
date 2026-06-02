using System;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;

using AvClientMvvmContract.Campaign;
using AvClientMvvmContract.Generic;
using AvClientMvvmContract.Multiplayer;
using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.Multiplayer.GameLobby;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView.Generic;

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
        // LoadingScreen fills the Grid (1280x720). MainMenu will be centered.
        MainContent.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        MainContent.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

        loadingScreen = new LoadingScreen();
        loadingScreen.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        loadingScreen.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
        loadingScreen.Completed += OnLoadingCompleted;
        loadingScreen.Loaded += (s, e) =>
        {
            var ls = (LoadingScreen)s!;
            Serilog.Log.Debug($"[DEBUG] LoadingScreen Loaded: Bounds={ls.Bounds}, DesiredSize={ls.DesiredSize}, Width={ls.Width}, Height={ls.Height}");
        };
        MainContent.Content = loadingScreen;

        mainMenu = new MainMenu();
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (ViewConstants.InitializeServices != null)
        {
            var init = ViewConstants.InitializeServices;
            await Task.Run(() =>
            {
                ViewConstants.ServiceProvider = init();
            });
        }

        // Back on UI thread — connect ViewModels
        await Dispatcher.UIThread.InvokeAsync(ConnectAfterInit);
    }

    private void ConnectAfterInit()
    {
        var sp = ViewConstants.ServiceProvider!;

        // Connect LoadingScreen ViewModel
        var loadingScreenVM = sp.GetRequiredService<ILoadingScreenViewModel>();
        loadingScreen!.ViewModel = loadingScreenVM;

        // Connect MainMenu ViewModel and all child ViewModels
        var mainMenuVM = sp.GetRequiredService<IMainMenuViewModel>();
        mainMenu!.ViewModel = mainMenuVM;
        mainMenu.SetCampaignSelectorViewModel(sp.GetRequiredService<ICampaignSelectorViewModel>());
        mainMenu.SetOptionsWindowViewModel(sp.GetRequiredService<IOptionsWindowViewModel>());
        mainMenu.SetExtrasWindowViewModel(sp.GetRequiredService<IExtrasWindowViewModel>());
        mainMenu.SetStatisticsWindowViewModel(sp.GetRequiredService<IStatisticsWindowViewModel>());
        mainMenu.SetGameLoadingWindowViewModel(sp.GetRequiredService<IGameLoadingWindowViewModel>());
        mainMenu.SetSkirmishLobbyViewModel(sp.GetRequiredService<ISkirmishLobbyViewModel>());
        mainMenu.SetCnCNetLobbyViewModel(sp.GetRequiredService<ICnCNetLobbyViewModel>());
        mainMenu.SetLANLobbyViewModel(sp.GetRequiredService<ILANLobbyViewModel>());
        mainMenu.SetPrivateMessagingWindowViewModel(sp.GetRequiredService<IPrivateMessagingWindowViewModel>());
        mainMenu.SetPrivacyNotificationViewModel(sp.GetRequiredService<IPrivacyNotificationViewModel>());
    }

    private void OnLoadingCompleted(object? sender, EventArgs e)
    {
        ((LoadingScreen)sender!).Completed -= OnLoadingCompleted;
        // TODO: should I fire loadingScreen.Completed in UIThread and therefore remove this Dispatcher call?
        Dispatcher.UIThread.Post(TransitionToMainMenu);
    }

    private void TransitionToMainMenu()
    {
        loadingScreen = null;
        MainContent.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        MainContent.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        MainContent.Content = mainMenu;
    }
}
