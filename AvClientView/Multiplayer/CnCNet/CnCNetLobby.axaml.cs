using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

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
        SetupChatInputEnterKey();
        SetupGameListHover();
    }

    private bool _infoPanelPositioned;

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BackgroundHelper.ApplyDefaultBackground(this, "cncnetlobbybg.png", IniOverlayService);
        var iniOverlay = IniOverlayService;
        iniOverlay?.ApplyLayout(this, "CnCNetLobby");

        if (!_infoPanelPositioned)
        {
            LayoutUpdated += PositionInfoPanel;
        }
    }

    private void PositionInfoPanel(object? sender, EventArgs e)
    {
        if (lbGameList == null || panelGameInfo == null) return;

        var bounds = lbGameList.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        Canvas.SetLeft(panelGameInfo, bounds.Right);
        Canvas.SetTop(panelGameInfo, bounds.Top);
        panelGameInfo.MaxHeight = bounds.Height;
        panelGameInfo.Width = bounds.Width;

        int maxZ = 0;
        foreach (var child in MainCanvas.Children)
        {
            if (child.ZIndex > maxZ)
                maxZ = child.ZIndex;
        }
        panelGameInfo.ZIndex = maxZ + 1;

        _infoPanelPositioned = true;
        LayoutUpdated -= PositionInfoPanel;
    }

    private void SetupGameListHover()
    {
        if (lbGames == null) return;

        lbGames.AddHandler(PointerMovedEvent, (s, e) =>
        {
            var vm = ViewModel;
            if (vm == null) return;

            var point = e.GetPosition(lbGames);
            const double itemHeight = 18.0;
            int index = (int)(point.Y / itemHeight);
            if (index < 0 || index >= vm.Games.Count)
                vm.HoveredGameIndex = -1;
            else
                vm.HoveredGameIndex = index;
        }, handledEventsToo: true);

        lbGames.AddHandler(PointerExitedEvent, (s, e) =>
        {
            var vm = ViewModel;
            if (vm != null)
                vm.HoveredGameIndex = -1;
        }, handledEventsToo: true);
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

    void ISwitchableView.Show() => IsVisible = true;
    void ISwitchableView.Hide() => IsVisible = false;
    string ISwitchableView.GetDisplayName() => "CnCNet Lobby";
}
