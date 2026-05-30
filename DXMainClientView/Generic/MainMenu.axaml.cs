using DXMainClientMVVMContract.Generic;
using DXMainClientMVVMContract.Generic.OptionPanels;
using DXMainClientMVVMContract.Campaign;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DXMainClientView.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView.Generic;

public partial class MainMenu : UserControl
{
    public MainMenu()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Set default background (matching original: AssetLoader.LoadTexture("MainMenu/mainmenubg.png"))
        ApplyDefaultBackground("MainMenu/mainmenubg.png");

        // Apply INI layout overrides (MainMenu.ini + GenericWindow.ini)
        var iniOverlay = App.ServiceProvider?.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "MainMenu");
    }

    private void ApplyDefaultBackground(string texturePath)
    {
        try
        {
            var iniOverlay = App.ServiceProvider?.GetService<IIniLayoutOverlayService>();
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
        topBar.ViewModel = topBarViewModel;
        topBarViewModel.PropertyChanged += OnTopBarPropertyChanged;
        UpdatePlayerCountVisibility(topBarViewModel.IsPlayerCountVisible);
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
    }

    public void SetOptionsWindowViewModel(IOptionsWindowViewModel vm)
    {
        optionsWindow.ViewModel = vm;
    }

    public void SetExtrasWindowViewModel(IExtrasWindowViewModel vm)
    {
        extrasWindow.ViewModel = vm;
    }

    public void SetGameLoadingWindowViewModel(IGameLoadingWindowViewModel vm)
    {
        gameLoadingWindow.ViewModel = vm;
    }
}
