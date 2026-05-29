using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ClientCore;
using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Online;
using DXMainClientViewModel.Online.EventArguments;

namespace DXMainClientViewModel.Multiplayer.CnCNet;


/// <summary>
/// ViewModel for the global context menu.
/// Contains all business logic from GlobalContextMenu.cs except XNA UI rendering.
/// </summary>
public partial class GlobalContextMenuViewModel : ObservableObject, IGlobalContextMenuViewModel
{
    private readonly CnCNetManager connectionManager;
    private readonly CnCNetUserData cncNetUserData;

    private GlobalContextMenuData? contextMenuData;
    private IRCUser? resolvedUser;

    // --- Observable state ---

    [ObservableProperty]
    private string _targetUserName = string.Empty;

    [ObservableProperty]
    private bool _canInvitePlayer;

    [ObservableProperty]
    private bool _canJoinPlayer;

    [ObservableProperty]
    private bool _canOpenPrivateMessage;

    [ObservableProperty]
    private bool _canAddFriend;

    [ObservableProperty]
    private bool _isFriend;

    [ObservableProperty]
    private bool _isBlocked;

    [ObservableProperty]
    private bool _isContextMenuVisible;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _menuLinks = new();
    public IReadOnlyList<string> MenuLinks => _menuLinks;

    private readonly Action<string>? onPrivateMessageRequested;
    private readonly Action<JoinUserEventArgs>? onJoinUserRequested;
    private readonly Action<string>? onInviteRequested;
    private readonly Action<string>? onCopyLinkRequested;
    private readonly Action<string>? onOpenLinkRequested;
    private readonly Action<string>? onCopyNameRequested;

    // --- Constructor ---

    public GlobalContextMenuViewModel(CnCNetManager connectionManager, CnCNetUserData cncNetUserData,
        Action<string>? onPrivateMessageRequested = null, Action<JoinUserEventArgs>? onJoinUserRequested = null,
        Action<string>? onInviteRequested = null, Action<string>? onCopyLinkRequested = null,
        Action<string>? onOpenLinkRequested = null, Action<string>? onCopyNameRequested = null)
    {
        this.connectionManager = connectionManager;
        this.cncNetUserData = cncNetUserData;
        this.onPrivateMessageRequested = onPrivateMessageRequested;
        this.onJoinUserRequested = onJoinUserRequested;
        this.onInviteRequested = onInviteRequested;
        this.onCopyLinkRequested = onCopyLinkRequested;
        this.onOpenLinkRequested = onOpenLinkRequested;
        this.onCopyNameRequested = onCopyNameRequested;
    }

    // --- Commands ---

    [RelayCommand]
    private void OpenPrivateMessage()
    {
        if (resolvedUser == null) return;
        IsContextMenuVisible = false;
        onPrivateMessageRequested?.Invoke(resolvedUser.Name);
    }

    [RelayCommand]
    private void InvitePlayer()
    {
        if (resolvedUser == null || contextMenuData == null) return;

        if (string.IsNullOrEmpty(contextMenuData.InviteChannelName) || ProgramConstants.IsInGame)
            return;

        string messageBody = ProgramConstants.GAME_INVITE_CTCP_COMMAND + " " + contextMenuData.InviteChannelName + ";" + contextMenuData.InviteGameName;

        if (!string.IsNullOrEmpty(contextMenuData.InviteChannelPassword))
            messageBody += ";" + contextMenuData.InviteChannelPassword;

        connectionManager.SendCustomMessage(new QueuedMessage(
            "PRIVMSG " + resolvedUser.Name + " :\u0001" + messageBody + "\u0001", QueuedMessageType.CHAT_MESSAGE, 0));

        IsContextMenuVisible = false;
        onInviteRequested?.Invoke(resolvedUser.Name);
    }

    [RelayCommand]
    private void JoinPlayer()
    {
        if (resolvedUser == null) return;
        IsContextMenuVisible = false;
        onJoinUserRequested?.Invoke(new JoinUserEventArgs(resolvedUser));
    }

    [RelayCommand]
    private void AddFriend()
    {
        if (resolvedUser == null) return;
        cncNetUserData.ToggleFriend(resolvedUser.Name);
        IsContextMenuVisible = false;
    }

    [RelayCommand]
    private void RemoveFriend()
    {
        if (resolvedUser == null) return;
        cncNetUserData.ToggleFriend(resolvedUser.Name);
        IsContextMenuVisible = false;
    }

    [RelayCommand]
    private void BlockUser()
    {
        if (resolvedUser == null) return;
        ToggleIgnoreUser(resolvedUser.Ident);
        IsContextMenuVisible = false;
    }

    [RelayCommand]
    private void UnblockUser()
    {
        if (resolvedUser == null) return;
        ToggleIgnoreUser(resolvedUser.Ident);
        IsContextMenuVisible = false;
    }

    [RelayCommand]
    private void CopyName()
    {
        if (resolvedUser == null) return;
        IsContextMenuVisible = false;
        onCopyNameRequested?.Invoke(resolvedUser.Name);
    }

    // --- Public methods ---

    /// <summary>
    /// Shows the context menu for the specified player name.
    /// </summary>
    public void Show(string playerName)
    {
        Show(new GlobalContextMenuData { PlayerName = playerName });
    }

    /// <summary>
    /// Shows the context menu for the specified IRC user.
    /// </summary>
    public void Show(IRCUser ircUser)
    {
        Show(new GlobalContextMenuData { IrcUser = ircUser });
    }

    /// <summary>
    /// Shows the context menu for the specified channel user.
    /// </summary>
    public void Show(ChannelUser channelUser)
    {
        Show(new GlobalContextMenuData { ChannelUser = channelUser });
    }

    /// <summary>
    /// Shows the context menu with the specified data.
    /// </summary>
    public void Show(GlobalContextMenuData data)
    {
        contextMenuData = data;
        resolvedUser = ResolveIrcUser(data);

        if (resolvedUser == null)
            return;

        UpdateButtons();
        UpdateLinks();
        IsContextMenuVisible = true;
    }

    /// <summary>
    /// Handles a link action (copy or open).
    /// </summary>
    public void HandleLinkAction(string link, bool open)
    {
        IsContextMenuVisible = false;
        if (open)
            onOpenLinkRequested?.Invoke(link);
        else
            onCopyLinkRequested?.Invoke(link);
    }

    // --- Helpers ---

    private void UpdateButtons()
    {
        var isOnline = resolvedUser != null && connectionManager.UserList.Any(u => u.Name == resolvedUser.Name);
        var isAdmin = contextMenuData?.ChannelUser?.IsAdmin ?? false;

        TargetUserName = resolvedUser?.Name ?? string.Empty;
        CanOpenPrivateMessage = resolvedUser != null && isOnline;
        CanAddFriend = resolvedUser != null;
        CanInvitePlayer = resolvedUser != null && isOnline && !string.IsNullOrEmpty(contextMenuData?.InviteChannelName);
        CanJoinPlayer = resolvedUser != null && !(contextMenuData?.PreventJoinGame ?? false) && isOnline;

        if (resolvedUser != null)
        {
            IsFriend = cncNetUserData.IsFriend(resolvedUser.Name);
            IsBlocked = cncNetUserData.IsIgnored(resolvedUser.Ident);
        }
    }

    private void UpdateLinks()
    {
        _menuLinks.Clear();

        var links = contextMenuData?.ChatMessage?.Message?.GetLinks();
        if (links == null)
            return;

        foreach (string link in links)
        {
            string displayLink = link;
            if (link.Length > 40)
                displayLink = link[..30] + "..." + link[^5..];

            if (!_menuLinks.Contains(displayLink))
                _menuLinks.Add(displayLink);
        }
    }

    private void ToggleIgnoreUser(string ident)
    {
        cncNetUserData.ToggleIgnoreUser(ident);
    }

    private IRCUser? ResolveIrcUser(GlobalContextMenuData data)
    {
        if (data.IrcUser != null)
            return data.IrcUser;

        if (data.ChannelUser?.IRCUser != null)
            return data.ChannelUser.IRCUser;

        if (!string.IsNullOrEmpty(data.PlayerName))
            return connectionManager.UserList.Find(u => u.Name == data.PlayerName);

        if (!string.IsNullOrEmpty(data.ChatMessage?.SenderName))
            return connectionManager.UserList.Find(u => u.Name == data.ChatMessage.SenderName);

        return null;
    }
}
// checked
