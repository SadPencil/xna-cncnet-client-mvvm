using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

using AvClientMvvmContract;
using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.ViewServices;

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
                vm.HoveredGameIndex = idx;
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
        RenderContextMenuItems(menu, ViewModel.PlayerContextMenuItems);
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

        chatList.DoubleTapped += (_, _) => ViewModel?.ChatMessageDoubleClickCommand.Execute(null);
    }

    private void ShowChatContextMenu(ContextRequestedEventArgs e)
    {
        if (ViewModel == null)
            return;

        SelectListBoxItemFromEvent(chatList, e);

        if (chatList.SelectedIndex < 0 || chatList.SelectedIndex >= ViewModel.ChatMessages.Count)
            return;

        var contextMenu = new ContextMenu();
        RenderContextMenuItems(contextMenu, ViewModel.ChatContextMenuItems);
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
        RenderContextMenuItems(contextMenu, ViewModel.GameContextMenuItems);
        contextMenu.Open(gameList);
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    /// <summary>
    /// Renders context menu items from a ViewModel-provided collection.
    /// Pure rendering — no business logic.
    /// </summary>
    private static void RenderContextMenuItems(ContextMenu menu, System.Collections.Generic.IReadOnlyList<IContextMenuItem> items)
    {
        menu.Items.Clear();
        foreach (var item in items)
        {
            if (item.IsSeparator)
                menu.Items.Add(new Separator());
            else if (item.IsVisible)
                menu.Items.Add(new MenuItem { Header = item.Text, Command = item.Command, IsEnabled = item.IsEnabled });
        }
    }

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
