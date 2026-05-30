using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DXMainClientView.Services;
using DXMainClientViewModel.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView.Generic;

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
        var iniOverlay = App.ServiceProvider?.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "OptionsWindow");
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

    public IOptionsWindowViewModel? ViewModel
    {
        get => DataContext as IOptionsWindowViewModel;
        set => DataContext = value;
    }
}
