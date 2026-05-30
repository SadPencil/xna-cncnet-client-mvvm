using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DXMainClientView.Campaign;
using DXMainClientView.Services;
using DXMainClientViewModel.Campaign;
using DXMainClientViewModel.Generic;
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

    // --- Child window wiring ---

    private ICampaignSelectorViewModel? campaignSelectorViewModel;
    private CampaignSelector? campaignSelectorWindow;

    /// <summary>
    /// Wires up the CampaignSelector ViewModel so the View observes IsVisible
    /// and shows/hides the CampaignSelector window accordingly.
    /// </summary>
    public void SetCampaignSelectorViewModel(ICampaignSelectorViewModel vm)
    {
        campaignSelectorViewModel = vm;
        vm.PropertyChanged += OnCampaignSelectorPropertyChanged;
    }

    private void OnCampaignSelectorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ICampaignSelectorViewModel.IsVisible))
            return;

        var vm = (ICampaignSelectorViewModel)sender!;
        Dispatcher.UIThread.Post(() =>
        {
            if (vm.IsVisible)
            {
                if (campaignSelectorWindow == null)
                {
                    campaignSelectorWindow = new CampaignSelector();
                    campaignSelectorWindow.ViewModel = vm;
                    campaignSelectorWindow.Closed += (_, _) => campaignSelectorWindow = null;
                }
                campaignSelectorWindow.Show();
            }
            else
            {
                campaignSelectorWindow?.Close();
            }
        });
    }
}
