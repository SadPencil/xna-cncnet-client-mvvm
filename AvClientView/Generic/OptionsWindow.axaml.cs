using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using AvClientMvvmContract.Generic;
using AvClientMvvmContract.Generic.OptionPanels;

using AvClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView.Generic;

public partial class OptionsWindow : UserControl, IOptionsWindowView
{
    public OptionsWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Apply default background (matching original: AssetLoader.LoadTextureUncached("optionsbg.png"))
        ApplyDefaultBackground("optionsbg.png");

        // Apply INI layout overrides (OptionsWindow.ini if it exists)
        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "OptionsWindow");

        // Wire up child panel DataContexts from DI
        SetupPanelDataContexts();
    }

    private void SetupPanelDataContexts()
    {
        var displayVM = ViewConstants.ServiceProvider.GetService<IDisplayOptionsPanelViewModel>();
        if (displayVM != null && displayPanel != null)
            displayPanel.DataContext = displayVM;

        var audioVM = ViewConstants.ServiceProvider.GetService<IAudioOptionsPanelViewModel>();
        if (audioVM != null && audioPanel != null)
            audioPanel.DataContext = audioVM;

        var gameVM = ViewConstants.ServiceProvider.GetService<IGameOptionsPanelViewModel>();
        if (gameVM != null && gamePanel != null)
            gamePanel.DataContext = gameVM;

        var cncnetVM = ViewConstants.ServiceProvider.GetService<ICnCNetOptionsPanelViewModel>();
        if (cncnetVM != null && cncnetPanel != null)
            cncnetPanel.DataContext = cncnetVM;

        var updaterVM = ViewConstants.ServiceProvider.GetService<IUpdaterOptionsPanelViewModel>();
        if (updaterVM != null && updaterPanel != null)
            updaterPanel.DataContext = updaterVM;

        var componentsVM = ViewConstants.ServiceProvider.GetService<IComponentsPanelViewModel>();
        if (componentsVM != null && componentsPanel != null)
            componentsPanel.DataContext = componentsVM;
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

    public IOptionsWindowViewModel? ViewModel
    {
        get => DataContext as IOptionsWindowViewModel;
        set => DataContext = value;
    }
}
