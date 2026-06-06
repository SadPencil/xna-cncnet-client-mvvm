using System;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;

using AvClientMvvmContract.Campaign;
using AvClientMvvmContract.Generic;
using AvClientMvvmContract.Multiplayer;
using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.Multiplayer.GameLobby;

using AvClientView.Services;

using ClientCore.Settings;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView.Generic;

public partial class MainWindow : Window
{
    private ServiceProvider? _serviceProvider;
    private LoadingScreen? _loadingScreen;
    private MainMenu? _mainMenu;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void ShowMainWindow()
    {
        // Apply borderless fullscreen immediately so the window doesn't
        // briefly appear in normal size before ConnectAfterInit runs.
        if (UserINISettings.Instance.BorderlessWindowedClient)
            WindowState = Avalonia.Controls.WindowState.FullScreen;

        // LoadingScreen fills the Grid (design resolution). MainMenu will be centered.
        MainContent.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        MainContent.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

        _loadingScreen = new LoadingScreen(iniOverlay: Startup.IniLayoutOverlayService);
        _loadingScreen.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        _loadingScreen.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
        _loadingScreen.Completed += OnLoadingCompleted;
        MainContent.Content = _loadingScreen;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var init = Startup.InitializeServices;
        await Task.Run(() =>
        {
            _serviceProvider = init();
        });

        // Back on UI thread — connect ViewModels
        await Dispatcher.UIThread.InvokeAsync(ConnectAfterInit, DispatcherPriority.Background);
    }

    private void ConnectAfterInit()
    {
        var sp = _serviceProvider!;
        var iniOverlay = sp.GetRequiredService<IIniLayoutOverlayService>();

        // Set MainWindow ViewModel (handles WindowState for minimize/restore)
        DataContext = sp.GetRequiredService<IMainWindowViewModel>();

        // Connect LoadingScreen ViewModel
        var loadingScreenVM = sp.GetRequiredService<ILoadingScreenViewModel>();
        _loadingScreen.ViewModel = loadingScreenVM;

        // MainMenu is created after DI is ready, so it receives a non-null INI service
        _mainMenu = new MainMenu(iniOverlay);

        // Connect MainMenu ViewModel and all child ViewModels
        var mainMenuVM = sp.GetRequiredService<IMainMenuViewModel>();
        _mainMenu.ViewModel = mainMenuVM;
        _mainMenu.SetCampaignSelectorViewModel(sp.GetRequiredService<ICampaignSelectorViewModel>());
        _mainMenu.SetOptionsWindowViewModel(sp.GetRequiredService<IOptionsWindowViewModel>());
        _mainMenu.SetExtrasWindowViewModel(sp.GetRequiredService<IExtrasWindowViewModel>());
        _mainMenu.SetStatisticsWindowViewModel(sp.GetRequiredService<IStatisticsWindowViewModel>());
        _mainMenu.SetGameLoadingWindowViewModel(sp.GetRequiredService<IGameLoadingWindowViewModel>());
        _mainMenu.SetSkirmishLobbyViewModel(sp.GetRequiredService<ISkirmishLobbyViewModel>());
        _mainMenu.SetCnCNetLobbyViewModel(sp.GetRequiredService<ICnCNetLobbyViewModel>());
        _mainMenu.SetLANLobbyViewModel(sp.GetRequiredService<ILANLobbyViewModel>());
        _mainMenu.SetPrivateMessagingWindowViewModel(sp.GetRequiredService<IPrivateMessagingWindowViewModel>());
        _mainMenu.SetPrivacyNotificationViewModel(sp.GetRequiredService<IPrivacyNotificationViewModel>());

        // GameInProgressWindow: full-screen overlay shown while game is running
        _mainMenu.SetGameInProgressWindowViewModel(sp.GetRequiredService<IGameInProgressWindowViewModel>());
    }

    private void OnLoadingCompleted(object? sender, EventArgs e)
    {
        ((LoadingScreen)sender!).Completed -= OnLoadingCompleted;
        TransitionToMainMenu();
    }

    private void TransitionToMainMenu()
    {
        _loadingScreen = null;
        MainContent.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        MainContent.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        MainContent.Content = _mainMenu;
    }
}
