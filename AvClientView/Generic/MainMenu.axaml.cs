using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using System.Collections.ObjectModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using AvClientMvvmContract;
using AvClientMvvmContract.Campaign;
using AvClientMvvmContract.Generic;
using AvClientMvvmContract.Generic.OptionPanels;
using AvClientMvvmContract.Messages;
using AvClientMvvmContract.Multiplayer;
using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.Multiplayer.GameLobby;

using AvClientView.Converters;
using AvClientView.Services;

using Serilog;

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;


namespace AvClientView.Generic;

public partial class MainMenu : UserControl
{
    private const int APPEAR_CURSOR_THRESHOLD_Y = 8;

    private readonly IIniLayoutOverlayService _iniOverlay;
    private double _menuWidth = 1280;
    private double _menuHeight = 720;
    private bool _isLoaded;
    private static int _dialogCounter;
    private readonly List<Action> _pendingDialogs = new();

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
        _iniOverlay.ApplyLayout(this, "MainMenu");

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
        foreach (var show in _pendingDialogs)
            show();
        _pendingDialogs.Clear();

        // ===== CONVERTER TEST: Direct test + visible ComboBox =====
        RunConverterTest();
    }

    private void RunConverterTest()
    {
        // Test 1: Direct converter call
        Log.Information("[LOG-CONV-TEST] === Direct converter test ===");
        var testColors = new (string Name, byte R, byte G, byte B)[]
        {
            ("Red", 255, 0, 0),
            ("Green", 0, 255, 0),
            ("Blue", 0, 0, 255),
            ("Yellow", 255, 255, 0),
        };
        foreach (var tc in testColors)
        {
            var testColor = new TestRgbColor(tc.Name, tc.R, tc.G, tc.B);
            var result = Rgb24ToBrushConverter.Instance.Convert(testColor, typeof(IBrush), null, System.Globalization.CultureInfo.CurrentCulture);
            Log.Information("[LOG-CONV-TEST] Direct: Name={Name}, R={R}, G={G}, B={B} -> Brush={Brush}, type={Type}",
                tc.Name, tc.R, tc.G, tc.B,
                result?.GetType().FullName ?? "null",
                result is ISolidColorBrush sb ? $"#{sb.Color.R:X2}{sb.Color.G:X2}{sb.Color.B:X2}" : "N/A");
        }

        // Test 2: Test different binding approaches on Foreground
        Log.Information("[LOG-CONV-TEST] === Binding approach tests ===");
        foreach (var approach in new[] { "empty-path", "dot-path", "no-converter-reflect" })
        {
            var testTb = new TextBlock();
            testTb.DataContext = new TestRgbColor("TestRed", 255, 0, 0);

            switch (approach)
            {
                case "empty-path":
                    // Approach A: empty path binding with converter
                    testTb.Bind(TextBlock.ForegroundProperty, new Binding { Converter = Rgb24ToBrushConverter.Instance });
                    Log.Information("[LOG-CONV-TEST] Approach 'empty-path': Bound with new Binding{{ Converter=... }}");
                    break;
                case "dot-path":
                    // Approach B: explicit "." path binding with converter
                    testTb.Bind(TextBlock.ForegroundProperty, new Binding(".") { Converter = Rgb24ToBrushConverter.Instance });
                    Log.Information("[LOG-CONV-TEST] Approach 'dot-path': Bound with new Binding(\".\"){{ Converter=... }}");
                    break;
                case "no-converter-reflect":
                    // Approach C: just set the Foreground directly using the converter
                    var brush = Rgb24ToBrushConverter.Instance.Convert(new TestRgbColor("TestRed", 255, 0, 0), typeof(IBrush), null, System.Globalization.CultureInfo.CurrentCulture) as IBrush;
                    testTb.Foreground = brush;
                    Log.Information("[LOG-CONV-TEST] Approach 'no-converter-reflect': Set Foreground directly to {Brush}", brush?.GetType().FullName ?? "null");
                    break;
            }

            Log.Information("[LOG-CONV-TEST] Result '{Name}': Foreground={FG}, type={T}",
                approach,
                testTb.Foreground is ISolidColorBrush sb3 ? $"#{sb3.Color.R:X2}{sb3.Color.G:X2}{sb3.Color.B:X2}" : testTb.Foreground?.ToString() ?? "null",
                testTb.Foreground?.GetType().FullName ?? "null");
        }

        // Test 3: Create a visible ComboBox with Runtime binding
        var testItems = new ObservableCollection<TestRgbColor>
        {
            new TestRgbColor("Red", 255, 0, 0),
            new TestRgbColor("Green", 0, 255, 0),
            new TestRgbColor("Blue", 0, 0, 255),
        };

        // Use non-generic FuncDataTemplate matching any object
        var dt = new FuncDataTemplate(typeof(object), (data, _) =>
        {
            Log.Information("[LOG-CONV-TEST] FuncDataTemplate BUILD called: data type={Type}, data={Data}",
                data?.GetType().FullName ?? "null", data?.ToString() ?? "null");
            var tb = new TextBlock();
            tb.DataContext = data;
            tb.Bind(TextBlock.TextProperty, new Binding("Name"));
            tb.Bind(TextBlock.ForegroundProperty, new Binding(".") { Converter = Rgb24ToBrushConverter.Instance });
            tb.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            tb.DataContextChanged += (s, e) =>
            {
                Log.Information("[LOG-CONV-TEST] DataTemplate TB DC set: type={Type}",
                    tb.DataContext?.GetType().FullName ?? "null");
            };
            tb.PropertyChanged += (s, e) =>
            {
                if (e.Property == TextBlock.ForegroundProperty)
                    Log.Information("[LOG-CONV-TEST] TB Foreground changed: val={Val}",
                        tb.Foreground is ISolidColorBrush sb2 ? $"#{sb2.Color.R:X2}{sb2.Color.G:X2}{sb2.Color.B:X2}" : tb.Foreground?.ToString() ?? "null");
            };
            return tb;
        });

        var testCombo = new ComboBox
        {
            Name = "TEST_ColorCombo",
            ItemsSource = testItems,
            ItemTemplate = dt,
            Width = 200, Height = 21,
            IsVisible = true,
        };
        IniLayoutProperties.SetSkipForeground(testCombo, true);
        Canvas.SetLeft(testCombo, 10);
        Canvas.SetTop(testCombo, 10);

        // Add to MainMenuPanel so it renders
        MainMenuPanel.Children.Add(testCombo);

        // Dump Foreground inheritance chain
        Log.Information("[LOG-CONV-TEST] --- Foreground chain ---");
        Log.Information("[LOG-CONV-TEST] testCombo.Foreground={FG}",
            testCombo.Foreground is ISolidColorBrush fgPb ? $"#{fgPb.Color.R:X2}{fgPb.Color.G:X2}{fgPb.Color.B:X2}" : testCombo.Foreground?.ToString() ?? "null");
        Log.Information("[LOG-CONV-TEST] this.Foreground (UserControl)={FG}",
            this.Foreground is ISolidColorBrush fgThis ? $"#{fgThis.Color.R:X2}{fgThis.Color.G:X2}{fgThis.Color.B:X2}" : this.Foreground?.ToString() ?? "null");

        // Check XnaTextBrush DynamicResource on this control
        if (this.FindResource("XnaTextBrush") is IBrush xnaB)
            Log.Information("[LOG-CONV-TEST] this.FindResource XnaTextBrush={FG}",
                xnaB is ISolidColorBrush fgXna ? $"#{fgXna.Color.R:X2}{fgXna.Color.G:X2}{fgXna.Color.B:X2}" : xnaB.ToString());
        else
            Log.Information("[LOG-CONV-TEST] XnaTextBrush NOT FOUND on this");

        // Check App resources
        if (Application.Current?.FindResource("XnaTextBrush") is IBrush appB)
            Log.Information("[LOG-CONV-TEST] App XnaTextBrush={FG}",
                appB is ISolidColorBrush fgApp ? $"#{fgApp.Color.R:X2}{fgApp.Color.G:X2}{fgApp.Color.B:X2}" : appB.ToString());

        // Open the dropdown programmatically to force item rendering
        Log.Information("[LOG-CONV-TEST] ComboBox added, IsVisible={IV}, ItemCount={IC}",
            testCombo.IsVisible, testCombo.ItemCount);
        testCombo.IsDropDownOpen = true;
        Log.Information("[LOG-CONV-TEST] IsDropDownOpen set to true");
        Dispatcher.UIThread.Post(() =>
        {
            Log.Information("[LOG-CONV-TEST] POST: IsDropDownOpen={IDO}, ItemCount={IC}",
                testCombo.IsDropDownOpen, testCombo.ItemCount);
        }, DispatcherPriority.Loaded);
    }

    public class TestRgbColor : IRgb24Color
    {
        public string Name { get; }
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public TestRgbColor(string name, byte r, byte g, byte b) { Name = name; R = r; G = g; B = b; }
        public override string ToString() => Name;
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
            var iniOverlay = _iniOverlay;
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

    private void ApplyDefaultBackgroundToPanel(Panel target, string texturePath)
    {
        try
        {
            var fullPath = _iniOverlay.FindTextureFile(texturePath);
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
            Content = "OK",
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
            Content = "Yes",
            Width = 80,
            Height = 23,
        };

        var noButton = new Button
        {
            Name = $"btnNo_{id}",
            Content = "No",
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
