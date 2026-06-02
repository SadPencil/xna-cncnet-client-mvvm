using AvClientMvvmContract.Multiplayer.CnCNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using AvClientViewModel.Online;
using AvClientViewModel.Online.EventArguments;

using Rampastring.Tools;
using AvClientMvvmContract.ViewServices;

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
    private int _selectedTabIndex;

    [ObservableProperty]
    private int _selectedUserIndex = -1;

    [ObservableProperty]
    private bool _isMessageInputEnabled;

    [ObservableProperty]
    private bool _isNotificationVisible;

    [ObservableProperty]
    private string _notificationSender = string.Empty;

    [ObservableProperty]
    private string _notificationMessage = string.Empty;

    [ObservableProperty]
    private string _playersLabelText = "PLAYERS:".L10N("Client:Main:Players");

    [ObservableProperty]
    private bool _isRecentPlayersVisible;

    [ObservableProperty]
    private bool _isMessagesPanelEnabled = true;

    [ObservableProperty]
    private bool _isVisible;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _userNames = new();
    public IReadOnlyList<string> UserNames => _userNames;

    private readonly ObservableCollection<string> _messageHistory = new();
    public IReadOnlyList<string> MessageHistory => _messageHistory;

    private readonly ObservableCollection<string> _recentPlayerNames = new();
    public IReadOnlyList<string> RecentPlayerNames => _recentPlayerNames;

    [ObservableProperty]
    private string _draftMessage = string.Empty;

    // --- Callbacks ---

    private readonly Action<string>? onSoundPlayRequested;

    // --- Constructor ---

    public PrivateMessagingWindowViewModel(
        CnCNetManager connectionManager,
        CnCNetUserData cncnetUserData,
        PrivateMessageHandler privateMessageHandler,
        IUIThreadMarshaller uiThreadMarshaller,
        IGameProcessService gameProcessService,
        Action<string>? onSoundPlayRequested = null)
    {
        this.connectionManager = connectionManager;
        this.cncnetUserData = cncnetUserData;
        this.privateMessageHandler = privateMessageHandler;
        this.uiThreadMarshaller = uiThreadMarshaller;
        this.gameProcessService = gameProcessService;
        this.onSoundPlayRequested = onSoundPlayRequested;
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
                Logger.Log("Null IRCUser in private messaging?");
                return;
            }

            pmUser = new PrivateMessageUser(iu);
            privateMessageUsers.Add(pmUser);
        }

        string sentMessage = $"[{ProgramConstants.PLAYERNAME}] {DraftMessage}";
        pmUser.Messages.Add(new ChatMessage(sentMessage));

        _messageHistory.Add(sentMessage);
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
            _messageHistory.Add(message.ToString());
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

        _messageHistory.Add(messageText);
        onSoundPlayRequested?.Invoke("message.wav");
    }

    private void ConnectionManager_UserAdded(object? sender, UserEventArgs e)
    {
        var pmUser = privateMessageUsers.Find(pmsgUser => pmsgUser.IrcUser.Name == e.User.Name);

        string? joinMessage = null;

        if (pmUser != null)
        {
            joinMessage = string.Format("{0} is now online.".L10N("Client:Main:PlayerOnline"), e.User.Name);
            pmUser.Messages.Add(new ChatMessage(joinMessage));
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

        string? leaveMessage = null;

        if (pmUser != null)
        {
            leaveMessage = string.Format("{0} is now offline.".L10N("Client:Main:PlayerOffline"), e.UserName);
            pmUser.Messages.Add(new ChatMessage(leaveMessage));
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
        uiThreadMarshaller.AddCallback(new Action(HandleGameProcessExited));
    }

    private void HandleGameProcessExited()
    {
        if (pmReceivedDuringGame != null)
        {
            ShowNotification(pmReceivedDuringGame.User, pmReceivedDuringGame.Message);
            pmReceivedDuringGame = null;
        }
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


