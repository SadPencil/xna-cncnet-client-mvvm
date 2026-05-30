using DXMainClientMvvmContract.Generic;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using DXMainClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView.Generic;

public partial class ExtrasWindow : UserControl, IExtrasWindowView
{
    public ExtrasWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Apply default background (matching original: extrasMenu.png -> MainMenu/mainmenuebg.png)
        ApplyDefaultBackground("MainMenu/mainmenuebg.png");

        // Apply INI layout overrides
        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "ExtrasWindow");
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

    public IExtrasWindowViewModel? ViewModel
    {
        get => DataContext as IExtrasWindowViewModel;
        set => DataContext = value;
    }
}
