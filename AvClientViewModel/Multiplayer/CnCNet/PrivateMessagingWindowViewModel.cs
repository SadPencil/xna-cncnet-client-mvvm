using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.Online;
using AvClientMvvmContract.ViewServices;

using AvClientViewModel.Online;
using AvClientViewModel.Online.EventArguments;
using AvClientViewModel.ViewServices;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;
using ClientCore.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Rampastring.Tools;

using Serilog;

namespace AvClientViewModel.Multiplayer.CnCNet;


/// <summary>
/// ViewModel for the private messaging window.
/// Contains all business logic from PrivateMessagingWindow.cs except XNA UI rendering.
/// </summary>
public partial class PrivateMessagingWindowViewModel : ObservableObject, IPrivateMessagingWindowViewModel
{
    private const int MESSAGES_INDEX = 0;
    private const int FRIEND_LIST_VIEW_INDEX = 1;
    private const int ALL_PLAYERS_VIEW_INDEX = 2;
    private const int RECENT_PLAYERS_VIEW_INDEX = 3;

    private readonly CnCNetManager connectionManager;
    private readonly CnCNetUserData cncnetUserData;
    private readonly PrivateMessageHandler privateMessageHandler;
    private readonly IUIThreadMarshaller uiThreadMarshaller;
    private readonly IGameProcessService gameProcessService;

    // --- State ---
    private readonly List<PrivateMessageUser> privateMessageUsers = new();
    private PrivateMessage? pmReceivedDuringGame;
    private string lastReceivedPMSender = string.Empty;
    private string lastConversationPartner = string.Empty;
    private string inviteChannelName = string.Empty;
    private string inviteGameName = string.Empty;
    private string inviteChannelPassword = string.Empty;

    // --- Observable state ---

    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedUserIndex { get; set; } = -1;

    [ObservableProperty]
    public partial bool IsMessageInputEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsNotificationVisible { get; set; }

    [ObservableProperty]
    public partial string NotificationSender { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NotificationMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PlayersLabelText { get; set; } = "PLAYERS:".L10N("Client:Main:Players");

    [ObservableProperty]
    public partial bool IsRecentPlayersVisible { get; set; }

    [ObservableProperty]
    public partial bool IsMessagesPanelEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    [ObservableProperty]
    public partial int SelectedMessageIndex { get; set; } = -1;

    [ObservableProperty]
    public partial int SelectedRecentPlayerIndex { get; set; } = -1;

    [ObservableProperty]
    public partial string? PendingLink { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<IContextMenuItem> UserContextMenuItems { get; set; } = Array.Empty<IContextMenuItem>();

    [ObservableProperty]
    public partial IReadOnlyList<IContextMenuItem> MessageContextMenuItems { get; set; } = Array.Empty<IContextMenuItem>();

    [ObservableProperty]
    public partial IReadOnlyList<IContextMenuItem> RecentPlayerContextMenuItems { get; set; } = Array.Empty<IContextMenuItem>();

    // --- Observable collections ---

    private readonly ObservableCollection<string> _userNames = new();
    public IReadOnlyList<string> UserNames => _userNames;

    private readonly ObservableCollection<IChatMessage> _messageHistory = new();
    public IReadOnlyList<IChatMessage> MessageHistory => _messageHistory;

    private readonly ObservableCollection<string> _recentPlayerNames = new();
    public IReadOnlyList<string> RecentPlayerNames => _recentPlayerNames;

    [ObservableProperty]
    public partial string DraftMessage { get; set; } = string.Empty;

    // --- Callbacks ---

    private readonly Action<string>? onSoundPlayRequested;
    private Action<string>? onJoinUserRequested;
    private readonly IClipboardService clipboardService;

    // --- Constructor ---

    public PrivateMessagingWindowViewModel(
        CnCNetManager connectionManager,
        CnCNetUserData cncnetUserData,
        PrivateMessageHandler privateMessageHandler,
        IUIThreadMarshaller uiThreadMarshaller,
        IGameProcessService gameProcessService,
        IClipboardService clipboardService,
        Action<string>? onSoundPlayRequested = null,
        Action<string>? onJoinUserRequested = null)
    {
        this.connectionManager = connectionManager;
        this.cncnetUserData = cncnetUserData;
        this.privateMessageHandler = privateMessageHandler;
        this.uiThreadMarshaller = uiThreadMarshaller;
        this.gameProcessService = gameProcessService;
        this.clipboardService = clipboardService;
        this.onSoundPlayRequested = onSoundPlayRequested;
        this.onJoinUserRequested = onJoinUserRequested;
    }

    // --- Commands ---

    [RelayCommand]
    private void SendMessage()
    {
        if (string.IsNullOrEmpty(DraftMessage))
            return;

        if (SelectedUserIndex < 0 || SelectedUserIndex >= _userNames.Count)
            return;

        string userName = _userNames[SelectedUserIndex];

        connectionManager.SendCustomMessage(new QueuedMessage("PRIVMSG " + userName + " :" + DraftMessage,
            QueuedMessageType.CHAT_MESSAGE, 0));

        PrivateMessageUser? pmUser = privateMessageUsers.Find(u => u.IrcUser.Name == userName);
        if (pmUser == null)
        {
            IRCUser? iu = connectionManager.UserList.Find(u => u.Name == userName);

            if (iu == null)
            {
                Log.Warning("Null IRCUser in private messaging?");
                return;
            }

            pmUser = new PrivateMessageUser(iu);
            privateMessageUsers.Add(pmUser);
        }

        string sentMessage = $"[{ProgramConstants.PLAYERNAME}] {DraftMessage}";
        var sentChatMessage = new ChatMessage(sentMessage);
        pmUser.Messages.Add(sentChatMessage);

        _messageHistory.Add(sentChatMessage);
        onSoundPlayRequested?.Invoke("message.wav");

        lastConversationPartner = userName;

        if (SelectedTabIndex != MESSAGES_INDEX)
        {
            SelectedTabIndex = MESSAGES_INDEX;
            SelectedUserIndex = FindUserIndexForName(userName);
        }

        DraftMessage = string.Empty;
    }

    [RelayCommand]
    private void Close()
    {
        IsVisible = false;
        IsNotificationVisible = false;
        privateMessageHandler.ResetUnreadMessageCount();
    }

    [RelayCommand]
    private void DismissNotification()
    {
        IsNotificationVisible = false;
        privateMessageHandler.IncrementUnreadMessageCount();
    }

    [RelayCommand]
    private void SwitchOn()
    {
        SelectedTabIndex = MESSAGES_INDEX;
        IsNotificationVisible = false;
        privateMessageHandler.ResetUnreadMessageCount();

        if (IsVisible)
        {
            if (!string.IsNullOrEmpty(lastReceivedPMSender))
            {
                int index = FindUserIndexForName(lastReceivedPMSender);
                if (index > -1)
                    SelectedUserIndex = index;
            }
        }
        else
        {
            IsVisible = true;

            if (!string.IsNullOrEmpty(lastConversationPartner))
            {
                int index = FindUserIndexForName(lastConversationPartner);
                if (index > -1)
                    SelectedUserIndex = index;
            }
        }
    }

    [RelayCommand]
    private void RefreshConversations()
    {
        RefreshUserList();
    }

    [RelayCommand]
    private void ToggleSelectedUserFriend()
    {
        var userName = GetSelectedUserName();
        if (userName == null)
            return;
        cncnetUserData.ToggleFriend(userName);
    }

    [RelayCommand]
    private void ToggleSelectedUserIgnore()
    {
        var userName = GetSelectedUserName();
        if (userName == null)
            return;
        var ident = connectionManager.UserList.Find(u => u.Name == userName)?.Ident;
        if (!string.IsNullOrEmpty(ident))
            cncnetUserData.ToggleIgnoreUser(ident);
    }

    [RelayCommand]
    private void JoinSelectedUserGame()
    {
        var userName = GetSelectedUserName();
        if (userName == null)
            return;
        onJoinUserRequested?.Invoke(userName);
    }

    [RelayCommand]
    private void InviteSelectedUserToGame()
    {
        var userName = GetSelectedUserName();
        if (userName == null || string.IsNullOrEmpty(inviteChannelName) || ProgramConstants.IsInGame)
            return;

        string messageBody = ProgramConstants.GAME_INVITE_CTCP_COMMAND + " "
            + inviteChannelName + ";" + inviteGameName;

        if (!string.IsNullOrEmpty(inviteChannelPassword))
            messageBody += ";" + inviteChannelPassword;

        connectionManager.SendCustomMessage(new QueuedMessage(
            "PRIVMSG " + userName + " :\u0001" + messageBody + "\u0001",
            QueuedMessageType.CHAT_MESSAGE, 0));
    }

    private string? GetSelectedUserName()
    {
        if (SelectedUserIndex < 0 || SelectedUserIndex >= _userNames.Count)
            return null;
        return _userNames[SelectedUserIndex];
    }

    // --- Message context menu commands ---

    [RelayCommand]
    private void OpenSelectedMessageSenderPrivateMessage()
    {
        if (SelectedMessageIndex < 0 || SelectedMessageIndex >= _messageHistory.Count)
            return;
        var msg = _messageHistory[SelectedMessageIndex];
        if (!string.IsNullOrEmpty(msg.SenderName))
            InitPM(msg.SenderName);
    }

    [RelayCommand]
    private void ToggleSelectedMessageSenderFriend()
    {
        if (SelectedMessageIndex < 0 || SelectedMessageIndex >= _messageHistory.Count)
            return;
        var msg = _messageHistory[SelectedMessageIndex];
        if (!string.IsNullOrEmpty(msg.SenderName))
            cncnetUserData.ToggleFriend(msg.SenderName);
    }

    [RelayCommand]
    private void ToggleSelectedMessageSenderIgnore()
    {
        if (SelectedMessageIndex < 0 || SelectedMessageIndex >= _messageHistory.Count)
            return;
        var msg = _messageHistory[SelectedMessageIndex];
        if (string.IsNullOrEmpty(msg.SenderIdent))
            return;
        cncnetUserData.ToggleIgnoreUser(msg.SenderIdent);
    }

    [RelayCommand]
    private void JoinSelectedMessageSenderGame()
    {
        if (SelectedMessageIndex < 0 || SelectedMessageIndex >= _messageHistory.Count)
            return;
        var msg = _messageHistory[SelectedMessageIndex];
        if (string.IsNullOrEmpty(msg.SenderName))
            return;
        onJoinUserRequested?.Invoke(msg.SenderName);
    }

    [RelayCommand]
    private void OpenPendingLink()
    {
        if (string.IsNullOrEmpty(PendingLink))
            return;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(PendingLink) { UseShellExecute = true }); }
        catch { }
    }

    [RelayCommand]
    private void CopyPendingLink()
    {
        if (string.IsNullOrEmpty(PendingLink))
            return;
        clipboardService.SetTextAsync(PendingLink);
    }

    // --- Recent player context menu commands ---

    private string? GetSelectedRecentPlayerName()
    {
        if (SelectedRecentPlayerIndex < 0 || SelectedRecentPlayerIndex >= _recentPlayerNames.Count)
            return null;
        return _recentPlayerNames[SelectedRecentPlayerIndex];
    }

    [RelayCommand]
    private void OpenSelectedRecentPlayerPrivateMessage()
    {
        var userName = GetSelectedRecentPlayerName();
        if (userName == null)
            return;
        InitPM(userName);
    }

    [RelayCommand]
    private void ToggleSelectedRecentPlayerFriend()
    {
        var userName = GetSelectedRecentPlayerName();
        if (userName == null)
            return;
        cncnetUserData.ToggleFriend(userName);
    }

    [RelayCommand]
    private void ToggleSelectedRecentPlayerIgnore()
    {
        var userName = GetSelectedRecentPlayerName();
        if (userName == null)
            return;
        var ident = connectionManager.UserList.Find(u => u.Name == userName)?.Ident;
        if (!string.IsNullOrEmpty(ident))
            cncnetUserData.ToggleIgnoreUser(ident);
    }

    [RelayCommand]
    private void JoinSelectedRecentPlayerGame()
    {
        var userName = GetSelectedRecentPlayerName();
        if (userName == null)
            return;
        onJoinUserRequested?.Invoke(userName);
    }

    // --- Lifecycle ---

    public void Initialize()
    {
        privateMessageHandler.PrivateMessageReceived += PrivateMessageHandler_PrivateMessageReceived;
        connectionManager.UserAdded += ConnectionManager_UserAdded;
        connectionManager.UserRemoved += ConnectionManager_UserRemoved;
        connectionManager.UserGameIndexUpdated += ConnectionManager_UserGameIndexUpdated;

        gameProcessService.GameProcessExited += GameProcessService_GameProcessExited;

        SelectedTabIndex = MESSAGES_INDEX;
    }

    public void InitPM(string name)
    {
        IsVisible = true;

        // Check if we've already talked with the user during this session
        int pmUserIndex = privateMessageUsers.FindIndex(
            pmUser => pmUser.IrcUser.Name == name);

        if (pmUserIndex > -1)
        {
            SelectedTabIndex = MESSAGES_INDEX;
            SelectedUserIndex = FindUserIndexForName(name);
            return;
        }

        if (cncnetUserData.IsFriend(name))
        {
            SelectedTabIndex = FRIEND_LIST_VIEW_INDEX;
        }
        else
        {
            SelectedTabIndex = ALL_PLAYERS_VIEW_INDEX;
        }

        SelectedUserIndex = FindUserIndexForName(name);
    }

    public void SetInviteChannelInfo(string channelName, string gameName, string channelPassword)
    {
        inviteChannelName = channelName;
        inviteGameName = gameName;
        inviteChannelPassword = channelPassword;
    }

    public void ClearInviteChannelInfo() => SetInviteChannelInfo(string.Empty, string.Empty, string.Empty);

    public void SetJoinUserAction(Action<string>? joinUserAction)
    {
        onJoinUserRequested = joinUserAction;
    }

    // --- Tab switching ---

    partial void OnSelectedTabIndexChanged(int value)
    {
        _userNames.Clear();
        _messageHistory.Clear();
        DraftMessage = string.Empty;

        switch (value)
        {
            case MESSAGES_INDEX:
                MessagesTabSelected();
                break;
            case FRIEND_LIST_VIEW_INDEX:
                FriendsListTabSelected();
                break;
            case ALL_PLAYERS_VIEW_INDEX:
                AllPlayersTabSelected();
                break;
            case RECENT_PLAYERS_VIEW_INDEX:
                RecentPlayersTabSelected();
                break;
        }
    }

    partial void OnSelectedUserIndexChanged(int value)
    {
        _messageHistory.Clear();
        DraftMessage = string.Empty;
        BuildUserContextMenuItems();

        if (value < 0 || value >= _userNames.Count)
        {
            IsMessageInputEnabled = false;
            return;
        }

        string userName = _userNames[value];
        IsMessageInputEnabled = IsPlayerOnline(userName);

        var pmUser = privateMessageUsers.Find(u => u.IrcUser.Name == userName);

        if (pmUser == null)
            return;

        foreach (ChatMessage message in pmUser.Messages)
        {
            _messageHistory.Add(message);
        }
    }

    // --- Tab content ---

    private void MessagesTabSelected()
    {
        IsRecentPlayersVisible = false;
        IsMessagesPanelEnabled = true;
        PlayersLabelText = "PLAYERS:".L10N("Client:Main:Players");

        var sortedUsers = privateMessageUsers
            .Select(pMsgUser => new
            {
                ircUser = pMsgUser.IrcUser,
                isFriend = cncnetUserData.FriendList.Contains(pMsgUser.IrcUser.Name),
                isOnline = connectionManager.UserList.Any(u => u.Name == pMsgUser.IrcUser.Name)
            })
            .OrderBy(u => !u.isOnline)
            .ThenBy(u => !u.isFriend)
            .ThenBy(u => u.ircUser.Name);

        foreach (var user in sortedUsers)
            _userNames.Add(user.ircUser.Name);
    }

    private void FriendsListTabSelected()
    {
        IsRecentPlayersVisible = false;
        IsMessagesPanelEnabled = true;
        PlayersLabelText = "PLAYERS:".L10N("Client:Main:Players");

        var friends = cncnetUserData.FriendList.Select(friendName =>
        {
            var ircUser = connectionManager.UserList.Find(u => u.Name == friendName);
            return new
            {
                ircUser = ircUser ?? new IRCUser(friendName),
                isOnline = ircUser != null
            };
        });

        friends
            .OrderBy(f => !f.isOnline)
            .ThenBy(f => f.ircUser.Name)
            .ToList()
            .ForEach(f => _userNames.Add(f.ircUser.Name));
    }

    private void AllPlayersTabSelected()
    {
        IsRecentPlayersVisible = false;
        IsMessagesPanelEnabled = true;
        PlayersLabelText = "PLAYERS:".L10N("Client:Main:Players");

        foreach (var user in connectionManager.UserList)
            _userNames.Add(user.Name);
    }

    private void RecentPlayersTabSelected()
    {
        IsRecentPlayersVisible = true;
        IsMessagesPanelEnabled = false;
        PlayersLabelText = "RECENT PLAYERS:".L10N("Client:Main:RecentPlayers");

        _recentPlayerNames.Clear();
        var recentPlayers = cncnetUserData.RecentList.OrderByDescending(rp => rp.GameTime);
        foreach (RecentPlayer recentPlayer in recentPlayers)
            _recentPlayerNames.Add(recentPlayer.PlayerName);
    }

    // --- Refresh ---

    private void RefreshUserList()
    {
        string selectedUserName = SelectedUserIndex >= 0 && SelectedUserIndex < _userNames.Count
            ? _userNames[SelectedUserIndex] : string.Empty;

        _userNames.Clear();

        switch (SelectedTabIndex)
        {
            case MESSAGES_INDEX:
                MessagesTabSelected();
                break;
            case FRIEND_LIST_VIEW_INDEX:
                FriendsListTabSelected();
                break;
            case ALL_PLAYERS_VIEW_INDEX:
                AllPlayersTabSelected();
                break;
        }

        SelectedUserIndex = FindUserIndexForName(selectedUserName);

        if (SelectedUserIndex < 0)
        {
            DraftMessage = string.Empty;
            IsMessageInputEnabled = false;
            _messageHistory.Clear();
        }
    }

    // --- Event handlers ---

    private void PrivateMessageHandler_PrivateMessageReceived(object? sender, PrivateMessageEventArgs e)
    {
        if (UserINISettings.Instance.AllowPrivateMessagesFromState == (int)AllowPrivateMessagesFromEnum.None)
            return;

        PrivateMessageUser? pmUser = privateMessageUsers.Find(u => u.IrcUser.Name == e.Sender);

        if (pmUser == null)
        {
            pmUser = new PrivateMessageUser(e.ircUser);
            privateMessageUsers.Add(pmUser);

            if (SelectedTabIndex == MESSAGES_INDEX)
            {
                string selectedUserName = SelectedUserIndex >= 0 && SelectedUserIndex < _userNames.Count
                    ? _userNames[SelectedUserIndex] : string.Empty;

                _userNames.Clear();
                privateMessageUsers.ForEach(pmsgUser =>
                    _userNames.Add(pmsgUser.IrcUser.Name));

                SelectedUserIndex = FindUserIndexForName(selectedUserName);
            }
        }

        bool isFriend = cncnetUserData.IsFriend(pmUser.IrcUser.Name);
        if (UserINISettings.Instance.AllowPrivateMessagesFromState == (int)AllowPrivateMessagesFromEnum.Friends && !isFriend)
            return;

        if (!isFriend &&
            UserINISettings.Instance.AllowPrivateMessagesFromState != (int)AllowPrivateMessagesFromEnum.All &&
            connectionManager.MainChannel.Users.Find(e.Sender) == null)
            return;

        string messageText = $"[{e.Sender}] {e.Message}";
        ChatMessage message = new ChatMessage(messageText);

        pmUser.Messages.Add(message);

        lastReceivedPMSender = e.Sender;
        lastConversationPartner = e.Sender;

        if (SelectedUserIndex < 0 || SelectedUserIndex >= _userNames.Count || _userNames[SelectedUserIndex] != e.Sender)
        {
            HandleNotification(pmUser.IrcUser, e.Message);
            return;
        }

        _messageHistory.Add(message);
        onSoundPlayRequested?.Invoke("message.wav");
    }

    private void ConnectionManager_UserAdded(object? sender, UserEventArgs e)
    {
        var pmUser = privateMessageUsers.Find(pmsgUser => pmsgUser.IrcUser.Name == e.User.Name);

        ChatMessage? joinMessage = null;

        if (pmUser != null)
        {
            joinMessage = new ChatMessage(string.Format("{0} is now online.".L10N("Client:Main:PlayerOnline"), e.User.Name));
            pmUser.Messages.Add(joinMessage);
        }

        if (SelectedTabIndex == ALL_PLAYERS_VIEW_INDEX)
        {
            RefreshAllUsers();
        }
        else
        {
            int userIndex = FindUserIndexForName(e.User.Name);
            if (userIndex >= 0 && userIndex == SelectedUserIndex)
            {
                IsMessageInputEnabled = true;

                if (joinMessage != null)
                    _messageHistory.Add(joinMessage);
            }
        }
    }

    private void ConnectionManager_UserRemoved(object? sender, UserNameIndexEventArgs e)
    {
        var pmUser = privateMessageUsers.Find(pmsgUser => pmsgUser.IrcUser.Name == e.UserName);

        ChatMessage? leaveMessage = null;

        if (pmUser != null)
        {
            leaveMessage = new ChatMessage(string.Format("{0} is now offline.".L10N("Client:Main:PlayerOffline"), e.UserName));
            pmUser.Messages.Add(leaveMessage);
        }

        if (SelectedTabIndex == ALL_PLAYERS_VIEW_INDEX)
        {
            int userIndex = FindUserIndexForName(e.UserName);
            if (userIndex >= 0)
            {
                if (userIndex == SelectedUserIndex)
                    SelectedUserIndex = -1;

                _userNames.RemoveAt(userIndex);
            }
        }
        else
        {
            int userIndex = FindUserIndexForName(e.UserName);
            if (userIndex >= 0 && userIndex == SelectedUserIndex)
            {
                IsMessageInputEnabled = false;
                if (leaveMessage != null)
                    _messageHistory.Add(leaveMessage);
            }
        }
    }

    private void ConnectionManager_UserGameIndexUpdated(object? sender, UserEventArgs e)
    {
        int userIndex = FindUserIndexForName(e.User.Name);
        if (userIndex >= 0)
        {
            // Force collection refresh so View picks up the updated game icon
            string name = _userNames[userIndex];
            _userNames[userIndex] = name;
        }
    }

    private void GameProcessService_GameProcessExited()
    {
        uiThreadMarshaller.AddCallback(() =>
        {
            if (pmReceivedDuringGame != null)
            {
                UI_ShowNotification(pmReceivedDuringGame.User, pmReceivedDuringGame.Message);
                pmReceivedDuringGame = null;
            }
        });
    }

    // --- Helpers ---

    private void HandleNotification(IRCUser ircUser, string message)
    {
        if (!ProgramConstants.IsInGame)
        {
            ShowNotification(ircUser, message);
        }
        else
        {
            pmReceivedDuringGame = new PrivateMessage(ircUser, message);
        }
    }

    private void ShowNotification(IRCUser ircUser, string message)
    {
        UI_ShowNotification(ircUser, message);
    }

    private void UI_ShowNotification(IRCUser ircUser, string message)
    {
        if (!UserINISettings.Instance.DisablePrivateMessagePopups)
        {
            NotificationSender = ircUser.Name;
            NotificationMessage = message;
            IsNotificationVisible = true;
        }
        else
        {
            privateMessageHandler.IncrementUnreadMessageCount();
        }

        onSoundPlayRequested?.Invoke("pm.wav");
    }

    private void RefreshAllUsers()
    {
        string selectedUserName = SelectedUserIndex >= 0 && SelectedUserIndex < _userNames.Count
            ? _userNames[SelectedUserIndex] : string.Empty;

        _userNames.Clear();

        foreach (var ircUser in connectionManager.UserList)
            _userNames.Add(ircUser.Name);

        SelectedUserIndex = FindUserIndexForName(selectedUserName);

        if (SelectedUserIndex < 0)
        {
            DraftMessage = string.Empty;
            IsMessageInputEnabled = false;
            _messageHistory.Clear();
        }
    }

    private bool IsPlayerOnline(string playerName) =>
        !string.IsNullOrEmpty(playerName) && connectionManager.UserList.Find(u => u.Name == playerName) != null;

    private int FindUserIndexForName(string userName) =>
        _userNames.ToList().FindIndex(name => name == userName);

    // --- Context menu builder partial methods ---

    partial void OnSelectedMessageIndexChanged(int value)
    {
        BuildMessageContextMenuItems();
    }

    partial void OnSelectedRecentPlayerIndexChanged(int value)
    {
        BuildRecentPlayerContextMenuItems();
    }

    private void BuildUserContextMenuItems()
    {
        var items = new List<IContextMenuItem>();
        int idx = SelectedUserIndex;
        if (idx < 0 || idx >= _userNames.Count)
        {
            UserContextMenuItems = items;
            return;
        }

        string userName = _userNames[idx];
        bool isFriend = cncnetUserData.IsFriend(userName);
        var ircUser = connectionManager.UserList.Find(u => u.Name == userName);
        bool isOnline = ircUser != null;
        bool isIgnored = !string.IsNullOrEmpty(ircUser?.Ident) && cncnetUserData.IsIgnored(ircUser.Ident);
        bool showInvite = !string.IsNullOrEmpty(inviteChannelName) && !ProgramConstants.IsInGame;

        if (isOnline)
            items.Add(new ContextMenuItem("Private Message".L10N("Client:Main:PrivateMessage"),
                OpenSelectedMessageSenderPrivateMessageCommand));

        items.Add(new ContextMenuItem(
            isFriend ? "Remove Friend".L10N("Client:Main:RemoveFriend") : "Add Friend".L10N("Client:Main:AddFriend"),
            ToggleSelectedUserFriendCommand));

        items.Add(new ContextMenuItem(
            isIgnored ? "Unblock".L10N("Client:Main:Unblock") : "Block".L10N("Client:Main:Block"),
            ToggleSelectedUserIgnoreCommand));

        if (showInvite && isOnline)
        {
            items.Add(new ContextMenuItem("", IsSeparator: true));
            items.Add(new ContextMenuItem("Invite".L10N("Client:Main:Invite"),
                InviteSelectedUserToGameCommand));
        }

        if (isOnline)
        {
            items.Add(new ContextMenuItem("", IsSeparator: true));
            items.Add(new ContextMenuItem("Join".L10N("Client:Main:Join"),
                JoinSelectedUserGameCommand));
        }

        UserContextMenuItems = items;
    }

    private void BuildMessageContextMenuItems()
    {
        var items = new List<IContextMenuItem>();
        int idx = SelectedMessageIndex;
        if (idx < 0 || idx >= _messageHistory.Count)
        {
            MessageContextMenuItems = items;
            return;
        }

        var msg = _messageHistory[idx];

        if (!string.IsNullOrEmpty(msg.SenderName))
        {
            var ircUser = connectionManager.UserList.Find(u => u.Name == msg.SenderName);
            bool isOnline = ircUser != null;
            bool isFriend = cncnetUserData.IsFriend(msg.SenderName);
            bool isIgnored = !string.IsNullOrEmpty(msg.SenderIdent) && cncnetUserData.IsIgnored(msg.SenderIdent);

            if (isOnline)
                items.Add(new ContextMenuItem("Private Message".L10N("Client:Main:PrivateMessage"),
                    OpenSelectedMessageSenderPrivateMessageCommand));

            items.Add(new ContextMenuItem(
                isFriend ? "Remove Friend".L10N("Client:Main:RemoveFriend") : "Add Friend".L10N("Client:Main:AddFriend"),
                ToggleSelectedMessageSenderFriendCommand));

            items.Add(new ContextMenuItem(
                isIgnored ? "Unblock".L10N("Client:Main:Unblock") : "Block".L10N("Client:Main:Block"),
                ToggleSelectedMessageSenderIgnoreCommand));

            if (isOnline)
            {
                items.Add(new ContextMenuItem("", IsSeparator: true));
                items.Add(new ContextMenuItem("Join".L10N("Client:Main:Join"),
                    JoinSelectedMessageSenderGameCommand));
            }
        }

        // Link operations
        var links = msg.Message.GetLinks();
        if (links != null && links.Length > 0)
        {
            if (items.Count > 0)
                items.Add(new ContextMenuItem("", IsSeparator: true));

            foreach (string link in links)
            {
                string displayLink = link.Length > 40 ? link[..30] + "..." + link[^5..] : link;

                items.Add(new ContextMenuItem(
                    string.Format("Open Link {0}".L10N("Client:Main:OpenLink"), displayLink),
                    new RelayCommand(() =>
                    {
                        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(link) { UseShellExecute = true }); }
                        catch { }
                    })));

                items.Add(new ContextMenuItem(
                    string.Format("Copy Link {0}".L10N("Client:Main:CopyLink"), displayLink),
                    new RelayCommand(() =>
                    {
                        try { clipboardService.SetTextAsync(link); }
                        catch { }
                    })));
            }
        }

        MessageContextMenuItems = items;
    }

    private void BuildRecentPlayerContextMenuItems()
    {
        var items = new List<IContextMenuItem>();
        int idx = SelectedRecentPlayerIndex;
        if (idx < 0 || idx >= _recentPlayerNames.Count)
        {
            RecentPlayerContextMenuItems = items;
            return;
        }

        string playerName = _recentPlayerNames[idx];
        var ircUser = connectionManager.UserList.Find(u => u.Name == playerName);
        bool isOnline = ircUser != null;
        bool isFriend = cncnetUserData.IsFriend(playerName);
        bool isIgnored = !string.IsNullOrEmpty(ircUser?.Ident) && cncnetUserData.IsIgnored(ircUser.Ident);

        if (isOnline)
            items.Add(new ContextMenuItem("Private Message".L10N("Client:Main:PrivateMessage"),
                OpenSelectedRecentPlayerPrivateMessageCommand));

        items.Add(new ContextMenuItem(
            isFriend ? "Remove Friend".L10N("Client:Main:RemoveFriend") : "Add Friend".L10N("Client:Main:AddFriend"),
            ToggleSelectedRecentPlayerFriendCommand));

        items.Add(new ContextMenuItem(
            isIgnored ? "Unblock".L10N("Client:Main:Unblock") : "Block".L10N("Client:Main:Block"),
            ToggleSelectedRecentPlayerIgnoreCommand));

        if (isOnline)
        {
            items.Add(new ContextMenuItem("", IsSeparator: true));
            items.Add(new ContextMenuItem("Join".L10N("Client:Main:Join"),
                JoinSelectedRecentPlayerGameCommand));
        }

        RecentPlayerContextMenuItems = items;
    }

    // --- Nested types ---

    private class PrivateMessage
    {
        public IRCUser User;
        public string Message;

        public PrivateMessage(IRCUser user, string message)
        {
            User = user;
            Message = message;
        }
    }
}


