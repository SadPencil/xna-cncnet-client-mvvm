using Avalonia;
using Avalonia.Controls;

using AvClientMvvmContract.Multiplayer;

using AvClientView.Controls;
using AvClientView.Services;


namespace AvClientView.Multiplayer;

public partial class LANLobby : UserControl, ILANLobbyView
{
    public AvClientView.Services.IIniLayoutOverlayService? IniOverlayService { get; set; }

    public LANLobby()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BackgroundHelper.ApplyDefaultBackground(this, "cncnetlobbybg.png", IniOverlayService);

        var iniOverlay = IniOverlayService;
        iniOverlay?.ApplyLayout(this, "LANLobby");

        // Wire up child overlay visibility
        WireOverlayVisibility(gameCreationWindow, gameCreationOverlay);
        WireOverlayVisibility(gameLobby, gameLobbyOverlay);
        WireOverlayVisibility(gameLoadingLobby, gameLoadingLobbyOverlay);
    }

    public ILANLobbyViewModel? ViewModel
    {
        get => DataContext as ILANLobbyViewModel;
        set => DataContext = value;
    }

    void ISwitchableView.Show() => IsVisible = true;
    void ISwitchableView.Hide() => IsVisible = false;
    string ISwitchableView.GetDisplayName() => "LAN Lobby";

    private static void WireOverlayVisibility(Control child, DarkeningPanel overlay)
    {
        child.PropertyChanged += (s, e) =>
        {
            if (e.Property == IsVisibleProperty)
                overlay.IsPanelVisible = child.IsVisible;
        };
        overlay.IsPanelVisible = child.IsVisible;
    }
}
