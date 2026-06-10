using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

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
        LobbyHelper.SetUpHoverTracking(gameList, idx =>
        {
            if (ViewModel is { } vm)
                vm.HoveredGameIndex = idx >= vm.Games.Count ? -1 : idx;
        });

        playerList.DoubleTapped += (_, _) => ViewModel?.OpenSelectedPlayerPrivateMessageCommand.Execute(null);
        SetupPlayerListContextMenu();
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BackgroundHelper.ApplyDefaultBackground(this, "cncnetlobbybg.png", IniOverlayService);
        LobbyHelper.AutoScrollToEnd(chatList);
        LobbyHelper.AutoScrollToEnd(playerList);
        LobbyHelper.AutoScrollToEnd(gameList);
    }

    public ICnCNetLobbyViewModel? ViewModel
    {
        get => DataContext as ICnCNetLobbyViewModel;
        set => DataContext = value;
    }

    void ISwitchableView.Show() => IsVisible = true;
    void ISwitchableView.Hide() => IsVisible = false;
    string ISwitchableView.GetDisplayName() => "CnCNet Lobby";

    private void SetupPlayerListContextMenu()
    {
        playerList.ContextRequested += (s, e) =>
        {
            e.Handled = true;
            ShowPlayerContextMenu(e);
        };
    }

    private void ShowPlayerContextMenu(ContextRequestedEventArgs e)
    {
        if (ViewModel == null)
            return;

        // Find the ListBoxItem that was right-clicked and select it
        if (e.Source is Control control)
        {
            var listBoxItem = control.FindAncestorOfType<ListBoxItem>();
            if (listBoxItem != null)
            {
                playerList.SelectedIndex = playerList.IndexFromContainer(listBoxItem);
            }
        }

        if (playerList.SelectedIndex < 0 || playerList.SelectedIndex >= ViewModel.Players.Count)
            return;

        var contextMenu = new ContextMenu();
        PopulatePlayerContextMenu(contextMenu);
        contextMenu.Open(playerList);
    }

    private void PopulatePlayerContextMenu(ContextMenu menu)
    {
        if (ViewModel == null)
            return;

        menu.Items.Clear();

        var pmItem = new MenuItem { Header = "Private Message" };
        pmItem.Click += (_, _) => ViewModel.OpenSelectedPlayerPrivateMessageCommand.Execute(null);
        menu.Items.Add(pmItem);

        menu.Items.Add(new Separator());

        var friendItem = new MenuItem { Header = "Toggle Friend" };
        friendItem.Click += (_, _) => ViewModel.ToggleSelectedPlayerFriendCommand.Execute(null);
        menu.Items.Add(friendItem);

        var blockItem = new MenuItem { Header = "Toggle Block" };
        blockItem.Click += (_, _) => ViewModel.ToggleSelectedPlayerIgnoreCommand.Execute(null);
        menu.Items.Add(blockItem);

        menu.Items.Add(new Separator());

        var joinItem = new MenuItem { Header = "Join Game" };
        joinItem.Click += (_, _) => ViewModel.JoinSelectedPlayerGameCommand.Execute(null);
        menu.Items.Add(joinItem);
    }
}
