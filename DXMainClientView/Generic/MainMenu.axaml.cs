using DXMainClientMvvmContract.Generic;
using DXMainClientMvvmContract.Generic.OptionPanels;
using DXMainClientMvvmContract.Campaign;
using DXMainClientMvvmContract.Multiplayer;
using DXMainClientMvvmContract.Multiplayer.CnCNet;
using DXMainClientMvvmContract.Multiplayer.GameLobby;

using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using DXMainClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView.Generic;

public partial class MainMenu : UserControl
{
    private const int APPEAR_CURSOR_THRESHOLD_Y = 8;
    private ITopBarViewModel? _topBarViewModel;

    public MainMenu()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        PointerMoved += OnPointerMoved;
        KeyDown += OnKeyDown;
        Focusable = true;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Set default background (matching original: AssetLoader.LoadTexture("MainMenu/mainmenubg.png"))
        ApplyDefaultBackground("MainMenu/mainmenubg.png");

        // Apply INI layout overrides (MainMenu.ini + GenericWindow.ini)
        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "MainMenu");

        // TEST: Create a border with ImageBrush to verify stretching
        AddImageBrushTest();

        // DIAGNOSTIC: Override INI leftbar background to solid red to check height
        var iniLeftbar = this.FindControl<Avalonia.Controls.Border>("leftbar");
        if (iniLeftbar != null)
        {
            System.Diagnostics.Debug.WriteLine($"INI leftbar BEFORE override: Width={iniLeftbar.Width}, Height={iniLeftbar.Height}, Bounds={iniLeftbar.Bounds}");
            iniLeftbar.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Red);
        }

        // Ensure we can receive keyboard input
        Focus();
    }

    private void AddImageBrushTest()
    {
        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        if (iniOverlay == null) return;
        var texturePath = iniOverlay.FindTextureFile("leftbar.png");
        if (texturePath == null) return;

        var bitmap = new Avalonia.Media.Imaging.Bitmap(texturePath);
        var brush = new Avalonia.Media.ImageBrush
        {
            Source = bitmap,
            Stretch = Avalonia.Media.Stretch.Fill,
            TileMode = Avalonia.Media.TileMode.FlipXY,
        };

        // Test 1: Border with ImageBrush Background (same as INI code)
        var testBrush = new Border
        {
            Name = "testImageBrush",
            Width = 24,
            Height = 696,
            Background = brush,
        };
        Avalonia.Controls.Canvas.SetLeft(testBrush, 110);
        Avalonia.Controls.Canvas.SetTop(testBrush, 12);
        MainCanvas.Children.Add(testBrush);

        // Test 2: Border with solid color (already exists as testBorder)
        // Test 3: Border with pre-stretched bitmap
        var stretched = new Avalonia.Media.Imaging.RenderTargetBitmap(
            new Avalonia.PixelSize(24, 696),
            new Avalonia.Vector(96, 96));
        using (var ctx = stretched.CreateDrawingContext())
        {
            ctx.DrawImage(bitmap, new Avalonia.Rect(0, 0, 24, 696));
        }
        var testStretched = new Border
        {
            Name = "testStretched",
            Width = 24,
            Height = 696,
            Background = new Avalonia.Media.ImageBrush
            {
                Source = stretched,
                Stretch = Avalonia.Media.Stretch.Fill,
                TileMode = Avalonia.Media.TileMode.FlipXY,
            },
        };
        Avalonia.Controls.Canvas.SetLeft(testStretched, 140);
        Avalonia.Controls.Canvas.SetTop(testStretched, 12);
        MainCanvas.Children.Add(testStretched);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var vm = ViewModel;
        if (vm == null || !vm.AreButtonsEnabled)
            return;

        // TODO: If the current panel is not the main menu, we don't want to trigger these shortcuts.
        // For now, I just disable all shortcuts.
        e.Handled = true;
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

    /// <summary>
    /// Sets the TopBar's ViewModel. Must be called before the control is shown.
    /// </summary>
    public void SetTopBarViewModel(ITopBarViewModel topBarViewModel)
    {
        _topBarViewModel = topBarViewModel;
        topBar.ViewModel = topBarViewModel;
        topBarViewModel.PropertyChanged += OnTopBarPropertyChanged;
        UpdatePlayerCountVisibility(topBarViewModel.IsPlayerCountVisible);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_topBarViewModel == null)
            return;

        var pos = e.GetPosition(this);
        if (pos.Y < APPEAR_CURSOR_THRESHOLD_Y)
            _topBarViewModel.ExpandCommand.Execute(null);
    }

    private void OnTopBarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ITopBarViewModel.IsPlayerCountVisible))
        {
            var topBarVM = (ITopBarViewModel)sender!;
            Dispatcher.UIThread.Post(() => UpdatePlayerCountVisibility(topBarVM.IsPlayerCountVisible));
        }
    }

    private void UpdatePlayerCountVisibility(bool isVisible)
    {
        if (lblCnCNetStatus != null)
            lblCnCNetStatus.IsVisible = isVisible;
        if (lblCnCNetPlayerCount != null)
            lblCnCNetPlayerCount.IsVisible = isVisible;
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
