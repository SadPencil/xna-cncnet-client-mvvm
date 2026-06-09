using Avalonia.Controls;
using Avalonia.Input;

using AvClientMvvmContract.Multiplayer.CnCNet;

using AvClientView.Controls;
using AvClientView.Services;

namespace AvClientView.Multiplayer.CnCNet;

public partial class CnCNetLobby : UserControl, ICnCNetLobbyView
{
    public IIniLayoutOverlayService? IniOverlayService { get; set; }

    public CnCNetLobby()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        gameList.DoubleTapped += (_, _) => ViewModel?.JoinSelectedGameCommand.Execute(null);
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BackgroundHelper.ApplyDefaultBackground(this, "cncnetlobbybg.png", IniOverlayService);
    }

    public ICnCNetLobbyViewModel? ViewModel
    {
        get => DataContext as ICnCNetLobbyViewModel;
        set => DataContext = value;
    }

    void ISwitchableView.Show() => IsVisible = true;
    void ISwitchableView.Hide() => IsVisible = false;
    string ISwitchableView.GetDisplayName() => "CnCNet Lobby";
}
