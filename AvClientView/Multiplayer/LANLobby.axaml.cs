using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

using AvClientMvvmContract.Multiplayer;

using AvClientView.Controls;
using AvClientView.Services;

namespace AvClientView.Multiplayer;

public partial class LANLobby : UserControl, ILANLobbyView
{
    public IIniLayoutOverlayService? IniOverlayService { get; set; }

    public LANLobby()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        gameList.DoubleTapped += (_, _) => ViewModel?.JoinSelectedGameCommand.Execute(null);
        SetUpHoverTracking();
    }

    private void SetUpHoverTracking()
    {
        gameList.AddHandler(PointerMovedEvent, (_, e) =>
        {
            if (ViewModel is not { } vm || vm.Games.Count == 0) return;
            var pos = e.GetPosition(gameList);
            const double approxRowHeight = 48.0;
            int idx = (int)(pos.Y / approxRowHeight);
            if (idx < 0) idx = 0;
            if (idx >= vm.Games.Count) idx = -1;
            vm.HoveredGameIndex = idx;
        }, handledEventsToo: true);

        gameList.AddHandler(PointerExitedEvent, (_, _) =>
        {
            if (ViewModel is { } vm)
                vm.HoveredGameIndex = -1;
        }, handledEventsToo: true);
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BackgroundHelper.ApplyDefaultBackground(this, "cncnetlobbybg.png", IniOverlayService);

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
