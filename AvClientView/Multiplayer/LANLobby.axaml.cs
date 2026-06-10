using Avalonia;
using Avalonia.Controls;

using AvClientMvvmContract.Multiplayer;

using AvClientView.Controls;
using AvClientView.Services;

using ClientCore.Extensions;

namespace AvClientView.Multiplayer;

public partial class LANLobby : UserControl, ILANLobbyView
{
    public IIniLayoutOverlayService? IniOverlayService { get; set; }

    public LANLobby()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        gameList.DoubleTapped += (_, _) => ViewModel?.JoinSelectedGameCommand.Execute(null);
        chatList.DoubleTapped += OnChatListDoubleTapped;
        LobbyHelper.SetUpHoverTracking(gameList, idx =>
        {
            if (ViewModel is { } vm)
                vm.HoveredGameIndex = idx >= vm.Games.Count ? -1 : idx;
        });
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BackgroundHelper.ApplyDefaultBackground(this, "cncnetlobbybg.png", IniOverlayService);
        LobbyHelper.AutoScrollToEnd(chatList);
        LobbyHelper.AutoScrollToEnd(playerList);
        LobbyHelper.AutoScrollToEnd(gameList);

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

    private void OnChatListDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (ViewModel == null) return;
        int idx = chatList.SelectedIndex;
        if (idx < 0 || idx >= ViewModel.ChatMessages.Count) return;
        var text = ViewModel.ChatMessages[idx];
        var links = text?.GetLinks();
        if (links == null || links.Length != 1) return;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(links[0]) { UseShellExecute = true }); }
        catch { }
    }

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
