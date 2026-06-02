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

using Serilog;

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

        // Apply default background to MainMenuPanel (the menu content area),
        // not the full UserControl. INI may later override this.
        ApplyDefaultBackgroundToPanel(MainMenuPanel, "MainMenu/mainmenubg.png");

        // Apply INI layout overrides (MainMenu.ini + GenericWindow.ini).
        // The [MainMenu] section's Size is meant for the main menu content area,
        // not the full UserControl which must stay at 1280x720 for overlays.
        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "MainMenu");

        // Steal the INI Size for MainMenuPanel and keep the UserControl at 1280x720.
        var menuWidth = Width;
        var menuHeight = Height;
        MainMenuPanel.Width = menuWidth;
        MainMenuPanel.Height = menuHeight;
        Width = 1280;
        Height = 720;

        // Center the panel when it's smaller than the full UserControl.
        // Must use Stretch in AXAML (not Center) so Canvas sizes correctly.
        if (menuWidth < 1280 || menuHeight < 720)
        {
            MainMenuPanel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            MainMenuPanel.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        }

        // Resize message box and yes/no dialog to cover the full menu panel
        messageBoxOverlay.Width = menuWidth;
        messageBoxOverlay.Height = menuHeight;
        yesNoDialogOverlay.Width = menuWidth;
        yesNoDialogOverlay.Height = menuHeight;

        // Apply INI BackgroundTexture to MainMenuPanel (it was applied to the
        // UserControl by ApplyLayout, but we want it on the content panel).
        if (Background is ImageBrush bgBrush)
        {
            MainMenuPanel.Background = Background;
            Background = null;
        }

        // Ensure we can receive keyboard input
        Focus();

        Serilog.Log.Debug($"[DEBUG] MainMenu.OnLoaded done: this.Width={Width}, this.Height={Height}, this.Bounds={Bounds}");
        Serilog.Log.Debug($"[DEBUG]   MainMenuPanel: Width={MainMenuPanel.Width}, Height={MainMenuPanel.Height}, Bounds={MainMenuPanel.Bounds}");
        Serilog.Log.Debug($"[DEBUG]   OverlayCanvas: Width={OverlayCanvas.Width}, Height={OverlayCanvas.Height}, Bounds={OverlayCanvas.Bounds}");
        Serilog.Log.Debug($"[DEBUG]   TopBar: Width={topBar.Width}, Height={topBar.Height}, Bounds={topBar.Bounds}, DataContext={topBar.DataContext?.GetType().Name}");
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

    private static void ApplyDefaultBackgroundToPanel(Panel target, string texturePath)
    {
        try
        {
            var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
            if (iniOverlay == null) return;
            var fullPath = iniOverlay.FindTextureFile(texturePath);
            if (fullPath != null)
            {
                var bitmap = new Bitmap(fullPath);
                target.Background = new ImageBrush { Source = bitmap, Stretch = Stretch.UniformToFill };
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
