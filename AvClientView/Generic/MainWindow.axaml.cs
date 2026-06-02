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
        // Show the loading screen immediately without a ViewModel.
        // The ServiceProvider is not ready yet — it will be built on a
        // background thread and assigned when done.
        loadingScreen = new LoadingScreen();
        loadingScreen.Width = 800;
        loadingScreen.Height = 600;
        loadingScreen.Completed += OnLoadingCompleted;
        MainContent.Content = loadingScreen;

        // Pre-create MainMenu (will be connected later after ServiceProvider is ready)
        mainMenu = new MainMenu();

        // Start heavy initialization on a background thread.
        // The loading screen is already visible while this runs.
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

        // Back on UI thread — connect ViewModel to loading screen
        await Dispatcher.UIThread.InvokeAsync(ConnectAfterInit);
    }

    private void ConnectAfterInit()
    {
        var sp = ViewConstants.ServiceProvider!;

        // Connect LoadingScreen ViewModel
        var loadingScreenVM = sp.GetRequiredService<ILoadingScreenViewModel>();
        loadingScreen!.ViewModel = loadingScreenVM;

        // Connect MainMenu ViewModel
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

        // Connect PrivacyNotification (overlay on top of other content)
        var privacyNotification = new PrivacyNotification
        {
            ViewModel = sp.GetRequiredService<IPrivacyNotificationViewModel>()
        };
        MainGrid.Children.Add(privacyNotification);
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
        MainContent.Content = mainMenu;
    }
}
