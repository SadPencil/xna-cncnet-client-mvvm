using AvMainClientMvvmContract.Multiplayer.CnCNet;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using AvMainClientView.Services;

using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;

namespace AvMainClientView.Multiplayer.CnCNet;

public partial class CnCNetLobby : UserControl, ICnCNetLobbyView
{
    public CnCNetLobby()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SetupChatInputEnterKey();
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ApplyDefaultBackground("cncnetlobbybg.png");

        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "CnCNetLobby");
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
        if (tbChatInput != null)
        {
            tbChatInput.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Return && ViewModel?.SendChatMessageCommand.CanExecute(null) == true)
                {
                    ViewModel.SendChatMessageCommand.Execute(null);
                    e.Handled = true;
                }
            };
        }
    }

    public ICnCNetLobbyViewModel? ViewModel
    {
        get => DataContext as ICnCNetLobbyViewModel;
        set => DataContext = value;
    }

    private void OnYesNoDialogYes(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // TODO: implement it in an MVVM way
        Debugger.Break();
    }

    private void OnYesNoDialogNo(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // TODO: implement it in an MVVM way
        Debugger.Break();
    }

    private void OnDismissMessage(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ViewModel?.DismissMessageCommand.Execute(null);
    }

    void ISwitchableView.Show() => IsVisible = true;
    void ISwitchableView.Hide() => IsVisible = false;
    string ISwitchableView.GetDisplayName() => "CnCNet Lobby";
}
