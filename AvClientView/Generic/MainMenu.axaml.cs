using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using AvClientMvvmContract.Campaign;
using AvClientMvvmContract.Generic;
using AvClientMvvmContract.Generic.OptionPanels;
using AvClientMvvmContract.Multiplayer;
using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.Multiplayer.GameLobby;

using AvClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView.Generic;

public partial class MainMenu : UserControl
{
    private const int APPEAR_CURSOR_THRESHOLD_Y = 8;

    public MainMenu()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        KeyDown += OnKeyDown;
        PointerMoved += OnPointerMoved;
        Focusable = true;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Wire TopBar ViewModel (DataContext set via AXAML binding, but code-behind also needs it)
        if (ViewModel != null)
            topBar.ViewModel = ViewModel.TopBarViewModel;

        // Set default background (matching original: AssetLoader.LoadTexture("MainMenu/mainmenubg.png"))
        ApplyDefaultBackground("MainMenu/mainmenubg.png");

        // Apply INI layout overrides (MainMenu.ini + GenericWindow.ini)
        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "MainMenu");


        // Ensure we can receive keyboard input
        Focus();
    }


    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var vm = ViewModel;
        if (vm == null || !vm.AreButtonsEnabled)
            return;

        // TODO: If the current panel is not the main menu, we don't want to trigger these shortcuts.
        // For now, I just disable all shortcuts.
        return;

        //switch (e.Key)
        //{
        //    case Key.C:
        //        vm.StartCampaignCommand.Execute(null);
        //        e.Handled = true;
        //        break;
        //    case Key.L:
        //        vm.LoadGameCommand.Execute(null);
        //        e.Handled = true;
        //        break;
        //    case Key.S:
        //        vm.StartSkirmishCommand.Execute(null);
        //        e.Handled = true;
        //        break;
        //    case Key.M:
        //        vm.JoinCnCNetCommand.Execute(null);
        //        e.Handled = true;
        //        break;
        //    case Key.N:
        //        vm.HostLANGameCommand.Execute(null);
        //        e.Handled = true;
        //        break;
        //    case Key.O:
        //        vm.OpenOptionsCommand.Execute(null);
        //        e.Handled = true;
        //        break;
        //    case Key.E:
        //        vm.OpenMapEditorCommand.Execute(null);
        //        e.Handled = true;
        //        break;
        //    case Key.T:
        //        vm.OpenStatisticsCommand.Execute(null);
        //        e.Handled = true;
        //        break;
        //    case Key.R:
        //        vm.OpenCreditsCommand.Execute(null);
        //        e.Handled = true;
        //        break;
        //    case Key.X:
        //        vm.OpenExtrasCommand.Execute(null);
        //        e.Handled = true;
        //        break;
        //}
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(this);
        if (pos.Y < APPEAR_CURSOR_THRESHOLD_Y)
            ViewModel?.TopBarViewModel.ExpandCommand.Execute(null);
    }

    private void ApplyDefaultBackground(string texturePath)
    {
        try
        {
            var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
            if (iniOverlay == null) return;
            var fullPath = iniOverlay.FindTextureFile(texturePath);
            if (fullPath != null)
            {
                var bitmap = new Bitmap(fullPath);
                Background = new ImageBrush { Source = bitmap, Stretch = Stretch.UniformToFill };
            }
        }
        catch { }
    }

    public IMainMenuViewModel? ViewModel
    {
        get => DataContext as IMainMenuViewModel;
        set => DataContext = value;
    }

    // --- Child window wiring (all are UserControls with IsVisible="{Binding IsVisible}") ---

    public void SetCampaignSelectorViewModel(ICampaignSelectorViewModel vm)
    {
        campaignSelector.ViewModel = vm;
        WireOverlayVisibility(campaignSelector, campaignOverlay);
    }

    public void SetOptionsWindowViewModel(IOptionsWindowViewModel vm)
    {
        optionsWindow.ViewModel = vm;
        WireOverlayVisibility(optionsWindow, optionsOverlay);
    }

    public void SetExtrasWindowViewModel(IExtrasWindowViewModel vm)
    {
        extrasWindow.ViewModel = vm;
        WireOverlayVisibility(extrasWindow, extrasOverlay);
    }

    public void SetStatisticsWindowViewModel(IStatisticsWindowViewModel vm)
    {
        statisticsWindow.ViewModel = vm;
        WireOverlayVisibility(statisticsWindow, statisticsOverlay);
    }

    public void SetGameLoadingWindowViewModel(IGameLoadingWindowViewModel vm)
    {
        gameLoadingWindow.ViewModel = vm;
        WireOverlayVisibility(gameLoadingWindow, gameLoadingOverlay);
    }

    public void SetSkirmishLobbyViewModel(ISkirmishLobbyViewModel vm)
    {
        skirmishLobby.ViewModel = vm;
        WireOverlayVisibility(skirmishLobby, skirmishOverlay);
    }

    public void SetCnCNetLobbyViewModel(ICnCNetLobbyViewModel vm)
    {
        cncNetLobby.ViewModel = vm;
        WireOverlayVisibility(cncNetLobby, cncNetOverlay);
    }

    public void SetLANLobbyViewModel(ILANLobbyViewModel vm)
    {
        lanLobby.ViewModel = vm;
        WireOverlayVisibility(lanLobby, lanOverlay);
    }

    public void SetPrivateMessagingWindowViewModel(IPrivateMessagingWindowViewModel vm)
    {
        privateMessagingWindow.ViewModel = vm;
        WireOverlayVisibility(privateMessagingWindow, pmOverlay);
    }

    public void SetPrivacyNotificationViewModel(IPrivacyNotificationViewModel vm)
    {
        privacyNotification.ViewModel = vm;
    }

    /// <summary>
    /// Binds a DarkeningPanel's visibility to a child view's IsVisible.
    /// Pure View-layer logic: observes one control's property, reflects to another.
    /// </summary>
    private static void WireOverlayVisibility(Control child, Controls.DarkeningPanel overlay)
    {
        child.PropertyChanged += (s, e) =>
        {
            if (e.Property == IsVisibleProperty)
                overlay.IsPanelVisible = child.IsVisible;
        };
        overlay.IsPanelVisible = child.IsVisible;
    }
}
