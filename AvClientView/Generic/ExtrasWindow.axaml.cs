using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using AvClientMvvmContract.Generic;

using AvClientView.Services;


namespace AvClientView.Generic;

public partial class ExtrasWindow : UserControl, IExtrasWindowView
{
    public AvClientView.Services.IIniLayoutOverlayService? IniOverlayService { get; set; }

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
        var iniOverlay = IniOverlayService;
        iniOverlay?.ApplyLayout(this, "ExtrasWindow");
    }

    private void ApplyDefaultBackground(string texturePath)
    {
        try
        {
            var iniOverlay = IniOverlayService;
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
