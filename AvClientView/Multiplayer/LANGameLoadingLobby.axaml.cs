using AvClientMvvmContract.Multiplayer;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using AvClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView.Multiplayer;

public partial class LANGameLoadingLobby : UserControl, ILANGameLoadingLobbyView
{
    public LANGameLoadingLobby()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SetupChatInputEnterKey();
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ApplyDefaultBackground("loadmpsavebg.png");

        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "GameLoadingLobby");
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

    public ILANGameLoadingLobbyViewModel? ViewModel
    {
        get => DataContext as ILANGameLoadingLobbyViewModel;
        set => DataContext = value;
    }

    IGameLoadingLobbyViewModel? IGameLoadingLobbyView.ViewModel
    {
        get => DataContext as IGameLoadingLobbyViewModel;
        set => DataContext = value;
    }

    void IGameLoadingLobbyView.Show() => IsVisible = true;
    void IGameLoadingLobbyView.Hide() => IsVisible = false;
}
