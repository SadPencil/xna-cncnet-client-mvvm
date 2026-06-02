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
        // LoadingScreen fills the design space. The MainMenu, which may be
        // smaller than 1280x720 when INI sets a smaller size, is centered.
        MainContent.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        MainContent.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

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
        // MainMenu is centered so smaller INI sizes (e.g. 800x600) don't stretch
        MainContent.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        MainContent.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        MainContent.Content = mainMenu;
    }
}
