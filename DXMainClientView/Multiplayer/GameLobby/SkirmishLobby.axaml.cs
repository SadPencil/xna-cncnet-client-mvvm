using DXMainClientMvvmContract.Multiplayer.GameLobby;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using DXMainClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView.Multiplayer.GameLobby;

public partial class SkirmishLobby : UserControl, ISkirmishLobbyView
{
    public SkirmishLobby()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ApplyDefaultBackground("gamelobbybg.png");

        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "SkirmishLobby");
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

    public IGameLobbyViewModel? ViewModel
    {
        get => DataContext as IGameLobbyViewModel;
        set => DataContext = value;
    }

    ISkirmishLobbyViewModel? ISkirmishLobbyView.ViewModel
    {
        get => DataContext as ISkirmishLobbyViewModel;
        set => DataContext = value;
    }

    void ISwitchableView.Show() => IsVisible = true;
    void ISwitchableView.Hide() => IsVisible = false;
    string ISwitchableView.GetDisplayName() => "Skirmish";
}
