using System.ComponentModel;

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
    private ILoadingScreenViewModel? loadingScreenVM;

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
        loadingScreenVM = App.ServiceProvider!.GetRequiredService<ILoadingScreenViewModel>();
        loadingScreen = new LoadingScreen();
        loadingScreen.ViewModel = loadingScreenVM;

        // Set design resolution for ViewBox scaling (INI overlay may override later)
        loadingScreen.Width = 800;
        loadingScreen.Height = 600;

        loadingScreenVM.PropertyChanged += OnLoadingScreenPropertyChanged;

        return loadingScreen;
    }

    private void OnLoadingScreenPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ILoadingScreenViewModel.IsLoading))
        {
            Dispatcher.UIThread.Post(HandleLoadingCompleted);
        }
    }

    private void HandleLoadingCompleted()
    {
        if (loadingScreenVM is not { IsLoading: false })
            return;

        loadingScreenVM.PropertyChanged -= OnLoadingScreenPropertyChanged;

        ShowPrivacyNotificationIfNeeded();
        TransitionToMainMenu();
    }

    private void ShowPrivacyNotificationIfNeeded()
    {
        if (loadingScreenVM?.ShouldShowPrivacyNotification != true)
            return;

        var overlayContent = this.FindControl<ContentControl>("OverlayContent");
        if (overlayContent == null)
            return;

        var privacyNotificationVM = App.ServiceProvider!.GetRequiredService<IPrivacyNotificationViewModel>();
        var privacyNotification = new PrivacyNotification();
        privacyNotification.ViewModel = privacyNotificationVM;
        overlayContent.Content = privacyNotification;

        privacyNotificationVM.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IPrivacyNotificationViewModel.IsVisible) &&
                privacyNotificationVM.IsVisible == false)
            {
                Dispatcher.UIThread.Post(() => overlayContent.Content = null);
            }
        };
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
        loadingScreen = null;
        loadingScreenVM = null;

        MainContent.Content = mainMenu;
    }
}
