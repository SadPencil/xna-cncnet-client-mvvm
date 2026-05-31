using DXMainClientMvvmContract.Multiplayer.GameLobby;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using DXMainClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView.Multiplayer.GameLobby;

public partial class LANGameLobby : UserControl, ILANGameLobbyView
{
    public LANGameLobby()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SetupChatInputEnterKey();
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ApplyDefaultBackground("gamelobbybg.png");

        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "MultiplayerGameLobby");
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

    private void SetupChatInputEnterKey()
    {
        tbChatInput.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter && ViewModel != null)
            {
                ViewModel.SendChatMessageCommand.Execute(null);
                e.Handled = true;
            }
        };
    }

    public ILANGameLobbyViewModel? ViewModel
    {
        get => DataContext as ILANGameLobbyViewModel;
        set => DataContext = value;
    }

    // Explicit interface implementations
    IMultiplayerGameLobbyViewModel? IMultiplayerGameLobbyView.ViewModel
    {
        get => DataContext as IMultiplayerGameLobbyViewModel;
        set => DataContext = value;
    }

    IGameLobbyViewModel? IGameLobbyView.ViewModel
    {
        get => DataContext as IGameLobbyViewModel;
        set => DataContext = value;
    }

    public void Show() => IsVisible = true;
    public void Hide() => IsVisible = false;
    public string GetDisplayName() => "LAN Game Lobby";
}
