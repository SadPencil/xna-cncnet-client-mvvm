using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using AvClientMvvmContract.Campaign;
using AvClientMvvmContract.Generic;
using AvClientMvvmContract.Generic.OptionPanels;
using AvClientMvvmContract.Messages;
using AvClientMvvmContract.Multiplayer;
using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.Multiplayer.GameLobby;

using AvClientView.Services;

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace AvClientView.Generic;

public partial class MainMenu : UserControl
{
    private const int APPEAR_CURSOR_THRESHOLD_Y = 8;

    private double _menuWidth = 1280;
    private double _menuHeight = 720;
    private bool _isLoaded;
    private static int _dialogCounter;
    private readonly List<Action> _pendingDialogs = new();

    public MainMenu()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        KeyDown += OnKeyDown;
        PointerMoved += OnPointerMoved;
        Focusable = true;

        // Register as recipient for dialog messages from ViewModels.
        // Dialogs arriving before OnLoaded are queued and shown after the panels are sized.
        WeakReferenceMessenger.Default.Register<OKDialogAsyncRequestMessage>(this, async (r, m) =>
        {
            m.Reply(Dispatcher.UIThread.InvokeAsync(() =>
            {
                var tcs = new TaskCompletionSource<OKDialogResult>();
                Action show = () =>
                {
                    var overlay = CreateOKDialogOverlay(m.Title, m.Message, () => tcs.SetResult(new OKDialogResult()));
                    OKDialogsPanel.Children.Add(overlay);
                };
                if (_isLoaded) show(); else _pendingDialogs.Add(show);
                return tcs.Task;
            }));
        });

        WeakReferenceMessenger.Default.Register<YesNoDialogAsyncRequestMessage>(this, async (r, m) =>
        {
            m.Reply(Dispatcher.UIThread.InvokeAsync(() =>
            {
                var tcs = new TaskCompletionSource<YesNoDialogResult>();
                Action show = () =>
                {
                    var overlay = CreateYesNoDialogOverlay(m.Title, m.Message, yes => tcs.SetResult(new YesNoDialogResult { Result = yes }));
                    YesNoDialogsPanel.Children.Add(overlay);
                };
                if (_isLoaded) show(); else _pendingDialogs.Add(show);
                return tcs.Task;
            }));
        });
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
        _menuWidth = menuWidth;
        _menuHeight = menuHeight;

        MainMenuPanel.Width = menuWidth;
        MainMenuPanel.Height = menuHeight;
        Width = 1280;
        Height = 720;

        // Size dialog overlay panels to cover the full menu panel
        OKDialogsPanel.Width = menuWidth;
        OKDialogsPanel.Height = menuHeight;
        YesNoDialogsPanel.Width = menuWidth;
        YesNoDialogsPanel.Height = menuHeight;

        // Center the panel when it's smaller than the full UserControl.
        // Must use Stretch in AXAML (not Center) so Canvas sizes correctly.
        if (menuWidth < 1280 || menuHeight < 720)
        {
            MainMenuPanel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            MainMenuPanel.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        }

        // Apply INI BackgroundTexture to MainMenuPanel (it was applied to the
        // UserControl by ApplyLayout, but we want it on the content panel).
        if (Background is ImageBrush bgBrush)
        {
            MainMenuPanel.Background = Background;
            Background = null;
        }

        // Ensure we can receive keyboard input
        Focus();

        // Drain any pending dialogs that arrived before the panels were sized
        _isLoaded = true;
        Log.Debug($"[DEBUG] MainMenu.OnLoaded: draining {_pendingDialogs.Count} pending dialogs");
        foreach (var show in _pendingDialogs)
            show();
        _pendingDialogs.Clear();

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

    #region Dialog Overlay Helpers

    /// <summary>
    /// Creates an OK dialog overlay matching the XNA message box visual style.
    /// INI layout is applied for theme colors, fonts, and button styling.
    /// </summary>
    private Border CreateOKDialogOverlay(string title, string message, Action onResult)
    {
        int id = System.Threading.Interlocked.Increment(ref _dialogCounter);

        var titleText = new TextBlock
        {
            Name = $"lblCaption_{id}",
            Text = title,
            FontWeight = FontWeight.Bold,
            Foreground = GetThemeBrush("XnaTextBrush", Brushes.Lime)
        };

        var messageText = new TextBlock
        {
            Name = $"lblDescription_{id}",
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = GetThemeBrush("XnaAltBrush", Brushes.Lime)
        };

        var okButton = new Button
        {
            Name = $"btnOK_{id}",
            Content = "OK",
            Width = 80,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var contentStack = new StackPanel { Spacing = 12 };
        contentStack.Children.Add(titleText);
        contentStack.Children.Add(messageText);
        contentStack.Children.Add(okButton);

        var innerBorder = new Border
        {
            Background = GetThemeBrush("XnaPanelBackgroundBrush", Brushes.Black),
            BorderBrush = GetThemeBrush("XnaPanelBorderBrush", Brushes.Cyan),
            BorderThickness = new Avalonia.Thickness(1),
            Padding = new Avalonia.Thickness(20),
            Width = 400,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Child = contentStack
        };

        var centerGrid = new Grid();
        centerGrid.Children.Add(innerBorder);

        var overlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            Child = centerGrid
        };

        bool handled = false;
        EventHandler onDismiss = (_, _) =>
        {
            if (handled) return;
            handled = true;
            if (overlay.Parent is Panel parent)
                parent.Children.Remove(overlay);
        };

        okButton.Click += (_, _) => { onDismiss(null!, EventArgs.Empty); onResult(); };

        ApplyIniLayout(overlay);

        Log.Debug($"[DEBUG] OKDialog_{id} created. innerBorder.Width={innerBorder.Width}, overlay.HAlign={overlay.HorizontalAlignment}");

        return overlay;
    }

    /// <summary>
    /// Creates a Yes/No dialog overlay matching the XNA message box visual style.
    /// INI layout is applied for theme colors, fonts, and button styling.
    /// </summary>
    private Border CreateYesNoDialogOverlay(string title, string message, Action<bool> onResult)
    {
        int id = System.Threading.Interlocked.Increment(ref _dialogCounter);

        var titleText = new TextBlock
        {
            Name = $"lblCaption_{id}",
            Text = title,
            FontWeight = FontWeight.Bold,
            Foreground = GetThemeBrush("XnaTextBrush", Brushes.Lime)
        };

        var messageText = new TextBlock
        {
            Name = $"lblDescription_{id}",
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = GetThemeBrush("XnaAltBrush", Brushes.Lime)
        };

        var yesButton = new Button
        {
            Name = $"btnYes_{id}",
            Content = "Yes",
            Width = 80
        };

        var noButton = new Button
        {
            Name = $"btnNo_{id}",
            Content = "No",
            Width = 80
        };

        var buttonStack = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 12
        };
        buttonStack.Children.Add(yesButton);
        buttonStack.Children.Add(noButton);

        var contentStack = new StackPanel { Spacing = 12 };
        contentStack.Children.Add(titleText);
        contentStack.Children.Add(messageText);
        contentStack.Children.Add(buttonStack);

        var innerBorder = new Border
        {
            Background = GetThemeBrush("XnaPanelBackgroundBrush", Brushes.Black),
            BorderBrush = GetThemeBrush("XnaPanelBorderBrush", Brushes.Cyan),
            BorderThickness = new Avalonia.Thickness(1),
            Padding = new Avalonia.Thickness(20),
            Width = 400,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Child = contentStack
        };

        var centerGrid = new Grid();
        centerGrid.Children.Add(innerBorder);

        var overlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            Child = centerGrid
        };

        bool handled = false;
        EventHandler onDismiss = (_, _) =>
        {
            if (handled) return;
            handled = true;
            if (overlay.Parent is Panel parent)
                parent.Children.Remove(overlay);
        };

        yesButton.Click += (_, _) => { onDismiss(null!, EventArgs.Empty); onResult(true); };
        noButton.Click += (_, _) => { onDismiss(null!, EventArgs.Empty); onResult(false); };

        ApplyIniLayout(overlay);

        Log.Debug($"[DEBUG] YesNoDialog_{id} created. innerBorder.Width={innerBorder.Width}, overlay.HAlign={overlay.HorizontalAlignment}");

        return overlay;
    }

    /// <summary>
    /// Applies INI layout (theme colors, fonts, button sizing, positioning)
    /// to a dialog overlay using the "MessageBox" section.
    /// </summary>
    private void ApplyIniLayout(Border overlay)
    {
        try
        {
            var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
            iniOverlay?.ApplyLayout(overlay, "MessageBox", _menuWidth, _menuHeight);
        }
        catch (Exception ex)
        {
            Log.Debug($"[DEBUG] ApplyIniLayout for MessageBox failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Safely looks up a theme resource, falling back to a default brush.
    /// </summary>
    private IBrush GetThemeBrush(string key, IBrush fallback)
    {
        try
        {
            if (this.FindResource(key) is IBrush brush)
                return brush;
        }
        catch
        {
        }
        return fallback;
    }

    #endregion
}
