using AvClientMvvmContract.Generic;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using AvClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView.Generic;

public partial class GameLoadingWindow : UserControl, IGameLoadingWindowView
{
    public GameLoadingWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Apply default background (matching original: loadmissionbg.png)
        ApplyDefaultBackground("loadmissionbg.png");

        // Apply INI layout overrides
        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "GameLoadingWindow");
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

    public IGameLoadingWindowViewModel? ViewModel
    {
        get => DataContext as IGameLoadingWindowViewModel;
        set => DataContext = value;
    }
}
