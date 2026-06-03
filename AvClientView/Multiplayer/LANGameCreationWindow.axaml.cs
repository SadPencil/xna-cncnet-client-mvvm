using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using AvClientMvvmContract.Multiplayer;

using AvClientView.Services;


namespace AvClientView.Multiplayer;

public partial class LANGameCreationWindow : UserControl, ILANGameCreationWindowView
{
    public AvClientView.Services.IIniLayoutOverlayService? IniOverlayService { get; set; }

    public LANGameCreationWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ApplyDefaultBackground("genericwindowbg.png");

        var iniOverlay = IniOverlayService;
        iniOverlay?.ApplyLayout(this, "GenericWindow");
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

    public ILANGameCreationWindowViewModel? ViewModel
    {
        get => DataContext as ILANGameCreationWindowViewModel;
        set => DataContext = value;
    }
}
