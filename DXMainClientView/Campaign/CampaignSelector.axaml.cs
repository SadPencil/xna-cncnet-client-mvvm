using DXMainClientMVVMContract.Campaign;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using DXMainClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView.Campaign;

public partial class CampaignSelector : UserControl
{
    public CampaignSelector()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Apply default background (matching original: AssetLoader.LoadTexture("MainMenu/dbak.png"))
        ApplyDefaultBackground("MainMenu/dbak.png");

        // Apply INI layout overrides (CampaignSelector.ini -> GenericWindow.ini)
        var iniOverlay = App.ServiceProvider?.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "CampaignSelector");
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

    public ICampaignSelectorViewModel? ViewModel
    {
        get => DataContext as ICampaignSelectorViewModel;
        set => DataContext = value;
    }
}
