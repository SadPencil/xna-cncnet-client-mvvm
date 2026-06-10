using System;
using System.Collections.Generic;
using System.IO;
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

using ClientCore.Extensions;
using AvClientMvvmContract.Messages;
using AvClientMvvmContract.Multiplayer;
using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.Multiplayer.GameLobby;

using AvClientView.Controls;
using AvClientView.Services;

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

using Serilog;


namespace AvClientView.Generic;

public partial class MainMenu : UserControl
{
    private const int APPEAR_CURSOR_THRESHOLD_Y = 8;

    private readonly IIniLayoutOverlayService _iniOverlay;
    private double _menuWidth = ViewConstants.DesignResolutionWidth;
    private double _menuHeight = ViewConstants.DesignResolutionHeight;
    private bool _isLoaded;
    private static int _dialogCounter;
    private readonly List<Action> _pendingDialogs = new();

    private Cursor? _customCursor;
    private IGameInProgressWindowViewModel? _gameInProgressVm;
    private double _lastRenderScale;
    private string? _lastCursorPath;

    public MainMenu(IIniLayoutOverlayService iniOverlay)
    {
        _iniOverlay = iniOverlay;
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

        // Clickable labels: forward PointerPressed to ViewModel commands.
        // Old client used XNALinkLabel (label with LeftClick + hover color).
        lblVersion.PointerPressed += (_, _) =>
            ViewModel?.OpenVersionCommand?.Execute(null);

        lblUpdateStatus.PointerPressed += (_, _) =>
        {
            if (lblUpdateStatus.IsEnabled)
                ViewModel?.UpdateStatusCommand?.Execute(null);
        };
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Wire TopBar ViewModel (DataContext set via AXAML binding, but code-behind also needs it)
        if (ViewModel != null)
            topBar.ViewModel = ViewModel.TopBarViewModel;

        // Apply default background to MainMenuPanel (the menu content area),
        // not the full UserControl. INI may later override this.
        BackgroundHelper.ApplyDefaultBackground(MainMenuPanel, "MainMenu/mainmenubg.png", _iniOverlay);

        // Apply INI layout overrides (MainMenu.ini + GenericWindow.ini).
        // The [MainMenu] section's Size is meant for the main menu content area,
        // not the full UserControl which must stay at design resolution for overlays.
        _iniOverlay.ApplyLayout(this, "MainMenu");

        // Steal the INI Size for MainMenuPanel and keep the UserControl at design resolution.
        var menuWidth = Width;
        var menuHeight = Height;
        _menuWidth = menuWidth;
        _menuHeight = menuHeight;

        MainMenuPanel.Width = menuWidth;
        MainMenuPanel.Height = menuHeight;
        Width = ViewConstants.DesignResolutionWidth;
        Height = ViewConstants.DesignResolutionHeight;

        // Size dialog overlay panels to cover the full menu panel
        OKDialogsPanel.Width = menuWidth;
        OKDialogsPanel.Height = menuHeight;
        YesNoDialogsPanel.Width = menuWidth;
        YesNoDialogsPanel.Height = menuHeight;

        // Pass INI service to child views
        campaignSelector.IniOverlayService = _iniOverlay;
        extrasWindow.IniOverlayService = _iniOverlay;
        statisticsWindow.IniOverlayService = _iniOverlay;
        gameLoadingWindow.IniOverlayService = _iniOverlay;
        skirmishLobby.IniOverlayService = _iniOverlay;
        cncNetLobby.IniOverlayService = _iniOverlay;
        lanLobby.IniOverlayService = _iniOverlay;
        privateMessagingWindow.IniOverlayService = _iniOverlay;
        optionsWindow.IniOverlayService = _iniOverlay;

        // Center the panel when it's smaller than the full UserControl.
        // Must use Stretch in AXAML (not Center) so Canvas sizes correctly.
        if (menuWidth < ViewConstants.DesignResolutionWidth || menuHeight < ViewConstants.DesignResolutionHeight)
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

        // Apply window icon and cursor from ViewModel
        ApplyWindowIconAndCursor();

        // Drain any pending dialogs that arrived before the panels were sized
        _isLoaded = true;
        foreach (var show in _pendingDialogs)
            show();
        _pendingDialogs.Clear();
    }


    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Shortcuts are disabled while the main menu is embedded in a sub-panel.
        // When re-enabled, bind to ViewModel commands via Window.KeyBindings instead.
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(this);
        if (pos.Y < APPEAR_CURSOR_THRESHOLD_Y)
            ViewModel?.TopBarViewModel.ExpandCommand.Execute(null);
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

    public void SetGameInProgressWindowViewModel(IGameInProgressWindowViewModel vm)
    {
        // Unsubscribe from previous ViewModel
        if (_gameInProgressVm != null)
            _gameInProgressVm.PropertyChanged -= OnGameInProgressPropertyChanged;

        _gameInProgressVm = vm;
        gameInProgressWindow.ViewModel = vm;
        WireOverlayVisibility(gameInProgressWindow, gameInProgressOverlay);

        // React to cursor visibility changes (e.g. game starts → hide cursor)
        if (vm != null)
            vm.PropertyChanged += OnGameInProgressPropertyChanged;
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

    /// <summary>
    /// Reacts to IsCursorVisible property changes from GameInProgressWindowViewModel.
    /// Hides cursor when game starts, shows it when game exits.
    /// </summary>
    private void OnGameInProgressPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IGameInProgressWindowViewModel.IsCursorVisible))
        {
            UpdateCursorVisibility(_gameInProgressVm?.IsCursorVisible ?? true);
        }
    }

    /// <summary>
    /// Applies the window icon and cursor from the ViewModel to the
    /// containing Window. Called once during OnLoaded after ViewModel is available.
    /// </summary>
    private void ApplyWindowIconAndCursor()
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || ViewModel == null)
            return;

        // Apply window icon from ViewModel-provided path
        string iconPath = ViewModel.WindowIconPath;
        if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
        {
            try
            {
                window.Icon = new WindowIcon(iconPath);
            }
            catch (Exception ex)
            {
                Log.Warning($"[MainMenu] Failed to load window icon from '{iconPath}': {ex.Message}");
            }
        }

        // Apply custom cursor from ViewModel-provided cursor.png path
        _lastCursorPath = ViewModel.CursorFilePath;
        _lastRenderScale = window.RenderScaling;
        LoadCustomCursor(_lastCursorPath, _lastRenderScale);
        if (_customCursor != null)
            window.Cursor = _customCursor;

        // Re-create cursor when DPI changes (e.g. window moved to a different monitor)
        window.LayoutUpdated += OnWindowLayoutUpdated;
    }

    /// <summary>
    /// Checks if the window's RenderScaling changed and reloads the cursor.
    /// Respects the current cursor visibility state from GameInProgressWindowViewModel.
    /// </summary>
    private void OnWindowLayoutUpdated(object? sender, EventArgs e)
    {
        if (sender is not Window window || _lastCursorPath == null)
            return;

        double newScale = window.RenderScaling;
        if (Math.Abs(newScale - _lastRenderScale) < 0.001)
            return;

        _lastRenderScale = newScale;
        LoadCustomCursor(_lastCursorPath, newScale);

        // Preserve current visibility state
        bool visible = _gameInProgressVm?.IsCursorVisible ?? true;
        UpdateCursorVisibility(visible);
    }

    /// <summary>
    /// Loads a custom cursor from a PNG image file, scaled to match the
    /// window's DPI/render scaling so the cursor appears at the correct size
    /// regardless of display scaling.
    /// The hotspot is (0, 0) to match the old XNA sprite cursor behavior.
    /// </summary>
    private void LoadCustomCursor(string cursorFilePath, double renderScale)
    {
        if (string.IsNullOrEmpty(cursorFilePath) || !File.Exists(cursorFilePath))
            return;

        try
        {
            using var original = new Bitmap(cursorFilePath);
            int w = original.PixelSize.Width;
            int h = original.PixelSize.Height;
            int newW = Math.Max(1, (int)(w * renderScale));
            int newH = Math.Max(1, (int)(h * renderScale));

            if (newW == w && newH == h)
            {
                _customCursor = new Cursor(original, new PixelPoint(0, 0));
            }
            else
            {
                var image = new Image { Source = original, Width = newW, Height = newH };
                image.Measure(new Size(newW, newH));
                image.Arrange(new Rect(0, 0, newW, newH));
                var scaled = new RenderTargetBitmap(new PixelSize(newW, newH));
                scaled.Render(image);
                _customCursor = new Cursor(scaled, new PixelPoint(0, 0));
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"[MainMenu] Failed to load cursor from '{cursorFilePath}': {ex.Message}");
        }
    }

    /// <summary>
    /// Toggles the Window cursor between the custom cursor and hidden.
    /// Called when IsCursorVisible changes on GameInProgressWindowViewModel.
    /// </summary>
    private void UpdateCursorVisibility(bool isVisible)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null)
            return;

        window.Cursor = isVisible ? (_customCursor ?? Cursor.Default) : new Cursor(StandardCursorType.None);
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
            Foreground = GetThemeBrush("XnaTextBrush", Brushes.Lime),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
        };

        var messageText = new TextBlock
        {
            Name = $"lblDescription_{id}",
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = GetThemeBrush("XnaAltBrush", Brushes.Lime),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
        };

        var okButton = new Button
        {
            Name = $"btnOK_{id}",
            Content = "OK".L10N("Client:UI:ButtonOK"),
            Width = 80,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Height = 23,
        };

        var contentStack = new StackPanel { Spacing = 12 };
        contentStack.Children.Add(titleText);
        contentStack.Children.Add(messageText);
        contentStack.Children.Add(okButton);

        // Force the content panel width so text wrapping is measured correctly.
        // 400 (innerBorder) - 2*1 (border) - 2*20 (padding) = 358
        contentStack.Width = 358;

        var innerBorder = new Border
        {
            Background = GetThemeBrush("XnaPanelBackgroundBrush", Brushes.Black),
            BorderBrush = GetThemeBrush("XnaPanelBorderBrush", Brushes.Cyan),
            BorderThickness = new Avalonia.Thickness(1),
            Padding = new Avalonia.Thickness(20),
            Width = 400,
            Child = contentStack
        };

        // Use Grid rows to reliably center the dialog vertically and horizontally
        var centerGrid = new Grid();
        centerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        centerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        centerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        centerGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        centerGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        centerGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        Grid.SetColumn(innerBorder, 1);
        Grid.SetRow(innerBorder, 1);
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

        ApplyButtonStyling(overlay);

        return overlay;
    }

    /// <summary>
    /// Creates a Yes/No dialog overlay matching the XNA message box visual style.
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
            Content = "Yes".L10N("Client:UI:ButtonYes"),
            Width = 80,
            Height = 23,
        };

        var noButton = new Button
        {
            Name = $"btnNo_{id}",
            Content = "No".L10N("Client:UI:ButtonNo"),
            Width = 80,
            Height = 23,
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
            Child = contentStack
        };

        // Use Grid rows/columns to reliably center the dialog in the overlay
        var centerGrid = new Grid();
        centerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        centerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        centerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        centerGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        centerGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        centerGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        Grid.SetColumn(innerBorder, 1);
        Grid.SetRow(innerBorder, 1);
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

        ApplyButtonStyling(overlay);

        return overlay;
    }

    /// <summary>
    /// Delegates to IIniLayoutOverlayService.ApplyStandardButtonStyling.
    /// </summary>
    private void ApplyButtonStyling(Control control)
    {
        _iniOverlay.ApplyStandardButtonStyling(control);
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
