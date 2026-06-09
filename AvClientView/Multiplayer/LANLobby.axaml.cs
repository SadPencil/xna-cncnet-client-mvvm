using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

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
        AutoScrollToEnd(chatList);
        AutoScrollToEnd(playerList);

        // Wire up child overlay visibility
        WireOverlayVisibility(gameCreationWindow, gameCreationOverlay);
        WireOverlayVisibility(gameLobby, gameLobbyOverlay);
        WireOverlayVisibility(gameLoadingLobby, gameLoadingLobbyOverlay);
    }

    private static void AutoScrollToEnd(ListBox listBox)
    {
        bool setup = false;
        void Setup()
        {
            if (setup) return;
            var sv = listBox.FindDescendantOfType<ScrollViewer>();
            if (sv is null) return;
            setup = true;

            bool isAtBottom = true;
            double prevExtent = 0;

            sv.ScrollChanged += (_, _) =>
            {
                bool wasAtBottom = isAtBottom;
                isAtBottom = sv.Offset.Y >= sv.Extent.Height - sv.Viewport.Height - 2;
                if (sv.Extent.Height > prevExtent && wasAtBottom)
                    sv.ScrollToEnd();
                prevExtent = sv.Extent.Height;
            };

            sv.ScrollToEnd();
        }

        listBox.TemplateApplied += (_, _) => Setup();
        listBox.LayoutUpdated += (_, _) => Setup();
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
