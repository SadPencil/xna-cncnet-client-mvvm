using Avalonia.Controls;

using AvClientMvvmContract.Multiplayer;

using AvClientView.Controls;
using AvClientView.Services;


namespace AvClientView.Multiplayer;

public partial class LANGameLoadingLobby : UserControl, ILANGameLoadingLobbyView
{
    public AvClientView.Services.IIniLayoutOverlayService? IniOverlayService { get; set; }

    public LANGameLoadingLobby()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BackgroundHelper.ApplyDefaultBackground(this, "loadmpsavebg.png", IniOverlayService);

        var iniOverlay = IniOverlayService;
        iniOverlay?.ApplyLayout(this, "GameLoadingLobby");
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
