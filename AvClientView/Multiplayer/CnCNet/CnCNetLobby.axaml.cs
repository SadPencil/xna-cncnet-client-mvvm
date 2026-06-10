using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

using AvClientMvvmContract.Multiplayer.CnCNet;

using AvClientView.Controls;
using AvClientView.Services;

using ClientCore.Extensions;

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
        SetupChatListContextMenu();
        SetupGameListContextMenu();
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

    // ========================================================================
    // Player list
    // ========================================================================

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

        SelectListBoxItemFromEvent(playerList, e);

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

        var pmItem = new MenuItem { Header = "Private Message".L10N("Client:UI:ContextMenuPrivateMessage") };
        pmItem.Click += (_, _) => ViewModel.OpenSelectedPlayerPrivateMessageCommand.Execute(null);
        menu.Items.Add(pmItem);

        menu.Items.Add(new Separator());

        var friendItem = new MenuItem { Header = "Toggle Friend".L10N("Client:UI:ContextMenuToggleFriend") };
        friendItem.Click += (_, _) => ViewModel.ToggleSelectedPlayerFriendCommand.Execute(null);
        menu.Items.Add(friendItem);

        var blockItem = new MenuItem { Header = "Toggle Block".L10N("Client:UI:ContextMenuToggleBlock") };
        blockItem.Click += (_, _) => ViewModel.ToggleSelectedPlayerIgnoreCommand.Execute(null);
        menu.Items.Add(blockItem);

        menu.Items.Add(new Separator());

        var inviteItem = new MenuItem { Header = "Invite to Game".L10N("Client:UI:ContextMenuInviteToGame") };
        inviteItem.Click += (_, _) => ViewModel.InviteSelectedPlayerToGameCommand.Execute(null);
        menu.Items.Add(inviteItem);

        var joinItem = new MenuItem { Header = "Join Game".L10N("Client:UI:ContextMenuJoinGame") };
        joinItem.Click += (_, _) => ViewModel.JoinSelectedPlayerGameCommand.Execute(null);
        menu.Items.Add(joinItem);
    }

    // ========================================================================
    // Chat messages
    // ========================================================================

    private void SetupChatListContextMenu()
    {
        chatList.ContextRequested += (s, e) =>
        {
            e.Handled = true;
            ShowChatContextMenu(e);
        };

        chatList.DoubleTapped += (_, _) =>
        {
            if (ViewModel == null) return;
            int idx = chatList.SelectedIndex;
            if (idx < 0 || idx >= ViewModel.ChatMessages.Count) return;
            var msg = ViewModel.ChatMessages[idx];
            var links = msg.Message.GetLinks();
            if (links != null && links.Length == 1)
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(links[0]) { UseShellExecute = true }); }
                catch { }
            }
        };
    }

    private void ShowChatContextMenu(ContextRequestedEventArgs e)
    {
        if (ViewModel == null)
            return;

        SelectListBoxItemFromEvent(chatList, e);

        if (chatList.SelectedIndex < 0 || chatList.SelectedIndex >= ViewModel.ChatMessages.Count)
            return;

        var msg = ViewModel.ChatMessages[chatList.SelectedIndex];

        var contextMenu = new ContextMenu();

        if (!string.IsNullOrEmpty(msg.SenderName))
        {
            var pmItem2 = new MenuItem { Header = "Private Message".L10N("Client:UI:ContextMenuPrivateMessage") };
            pmItem2.Click += (_, _) => ViewModel.OpenSelectedChatMessageSenderPrivateMessageCommand.Execute(null);
            contextMenu.Items.Add(pmItem2);

            contextMenu.Items.Add(new Separator());

            var friendItem2 = new MenuItem { Header = "Toggle Friend".L10N("Client:UI:ContextMenuToggleFriend") };
            friendItem2.Click += (_, _) => ViewModel.ToggleSelectedChatMessageSenderFriendCommand.Execute(null);
            contextMenu.Items.Add(friendItem2);

            var blockItem2 = new MenuItem { Header = "Toggle Block".L10N("Client:UI:ContextMenuToggleBlock") };
            blockItem2.Click += (_, _) => ViewModel.ToggleSelectedChatMessageSenderIgnoreCommand.Execute(null);
            contextMenu.Items.Add(blockItem2);

            contextMenu.Items.Add(new Separator());

            var joinItem2 = new MenuItem { Header = "Join Game".L10N("Client:UI:ContextMenuJoinGame") };
            joinItem2.Click += (_, _) => ViewModel.JoinSelectedChatMessageSenderGameCommand.Execute(null);
            contextMenu.Items.Add(joinItem2);
        }

        // Link operations from message body
        var links = msg.Message.GetLinks();
        if (links != null && links.Length > 0)
        {
            if (contextMenu.Items.Count > 0)
                contextMenu.Items.Add(new Separator());

            foreach (string link in links)
            {
                string displayLink = link.Length > 40 ? link[..30] + "..." + link[^5..] : link;

                var openLinkItem2 = new MenuItem { Header = string.Format("Open Link: {0}".L10N("Client:UI:OpenLink"), displayLink) };
                openLinkItem2.Click += (_, _) =>
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(link) { UseShellExecute = true }); }
                    catch { }
                };
                contextMenu.Items.Add(openLinkItem2);

                var copyLinkItem2 = new MenuItem { Header = string.Format("Copy Link: {0}".L10N("Client:UI:CopyLink"), displayLink) };
                copyLinkItem2.Click += (_, _) =>
                {
                    try { TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(link); }
                    catch { }
                };
                contextMenu.Items.Add(copyLinkItem2);
            }
        }

        if (contextMenu.Items.Count > 0)
            contextMenu.Open(chatList);
    }

    // ========================================================================
    // Game list
    // ========================================================================

    private void SetupGameListContextMenu()
    {
        gameList.ContextRequested += (s, e) =>
        {
            e.Handled = true;
            ShowGameContextMenu(e);
        };
    }

    private void ShowGameContextMenu(ContextRequestedEventArgs e)
    {
        if (ViewModel == null)
            return;

        SelectListBoxItemFromEvent(gameList, e);

        if (gameList.SelectedIndex < 0 || gameList.SelectedIndex >= ViewModel.Games.Count)
            return;

        var contextMenu = new ContextMenu();

        var pmItem3 = new MenuItem { Header = "Private Message".L10N("Client:UI:ContextMenuPrivateMessage") };
        pmItem3.Click += (_, _) => ViewModel.OpenSelectedGameHostPrivateMessageCommand.Execute(null);
        contextMenu.Items.Add(pmItem3);

        contextMenu.Items.Add(new Separator());

        var friendItem3 = new MenuItem { Header = "Toggle Friend".L10N("Client:UI:ContextMenuToggleFriend") };
        friendItem3.Click += (_, _) => ViewModel.ToggleSelectedGameHostFriendCommand.Execute(null);
        contextMenu.Items.Add(friendItem3);

        var blockItem3 = new MenuItem { Header = "Toggle Block".L10N("Client:UI:ContextMenuToggleBlock") };
        blockItem3.Click += (_, _) => ViewModel.ToggleSelectedGameHostIgnoreCommand.Execute(null);
        contextMenu.Items.Add(blockItem3);

        contextMenu.Items.Add(new Separator());

        var joinItem3 = new MenuItem { Header = "Join Game".L10N("Client:UI:ContextMenuJoinGame") };
        joinItem3.Click += (_, _) => ViewModel.JoinSelectedGameCommand.Execute(null);
        contextMenu.Items.Add(joinItem3);

        contextMenu.Open(gameList);
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    /// <summary>
    /// Finds the ListBoxItem under the pointer from a ContextRequested event
    /// and sets the ListBox's SelectedIndex to match.
    /// </summary>
    private static void SelectListBoxItemFromEvent(ListBox listBox, ContextRequestedEventArgs e)
    {
        if (e.Source is Control control)
        {
            var listBoxItem = control.FindAncestorOfType<ListBoxItem>();
            if (listBoxItem != null)
                listBox.SelectedIndex = listBox.IndexFromContainer(listBoxItem);
        }
    }
}
