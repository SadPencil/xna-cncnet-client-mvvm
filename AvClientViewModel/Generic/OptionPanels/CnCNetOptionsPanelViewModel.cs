using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using AvClientMvvmContract.Generic.OptionPanels;

using AvClientViewModel.Domain.Multiplayer.CnCNet;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvClientViewModel.Generic.OptionPanels;

/// <summary>
/// ViewModel for the CnCNet options panel.
/// Contains all business logic from CnCNetOptionsPanel.cs except XNA UI rendering.
/// </summary>
public partial class CnCNetOptionsPanelViewModel : ObservableObject, ICnCNetOptionsPanelViewModel
{
    private readonly UserINISettings iniSettings;
    private readonly GameCollection gameCollection;

    // AllowPrivateMessagesFrom dropdown: index -> enum value mapping
    // Order matches original XNA: All(1), CurrentChannel(4), Friends(2), None(3)
    private static readonly int[] PmModeByIndex = { 1, 4, 2, 3 };

    // --- Observable state ---

    [ObservableProperty]
    private bool _pingUnofficialTunnels;

    [ObservableProperty]
    private bool _writeInstallationPathToRegistry;

    [ObservableProperty]
    private bool _disableMainMenuHotkeys;

    [ObservableProperty]
    private bool _notifyOnUserListChanges;

    [ObservableProperty]
    private bool _disablePrivateMessagePopups;

    [ObservableProperty]
    private int _allowPrivateMessagesMode;

    [ObservableProperty]
    private int _selectedAllowPrivateMessagesFromIndex;

    public IReadOnlyList<string> AllowPrivateMessagesFromOptions { get; } = new[]
    {
        "All".L10N("Client:DTAConfig:PMAll"),
        "Current channel".L10N("Client:DTAConfig:PMCurrentChannel"),
        "Friends".L10N("Client:DTAConfig:PMFriends"),
        "None".L10N("Client:DTAConfig:PMNone"),
    };

    [ObservableProperty]
    private bool _skipLoginDialog;

    [ObservableProperty]
    private bool _persistentMode;

    [ObservableProperty]
    private bool _autoConnectOnStartup;

    [ObservableProperty]
    private bool _isDiscordIntegrationEnabled;

    [ObservableProperty]
    private bool _isSteamIntegrationEnabled;

    [ObservableProperty]
    private bool _allowGameInvitesOnlyFromFriends;

    [ObservableProperty]
    private bool _isAutoConnectOnStartupAllowed;

    [ObservableProperty]
    private bool _isDiscordIntegrationGloballyDisabled;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _followedGameNames = new();
    public IReadOnlyList<string> FollowedGameNames => _followedGameNames;

    // Store game data for View
    private readonly List<GameListItemData> _gameListItems = new();
    public IReadOnlyList<IGameListItemData> GameListItems => _gameListItems;

    // --- Constructor ---

    /// <summary>
    /// Indicates whether a restart is required after saving settings.
    /// </summary>
    public bool IsRestartRequired { get; private set; }

    public CnCNetOptionsPanelViewModel(UserINISettings iniSettings, GameCollection gameCollection)
    {
        this.iniSettings = iniSettings;
        this.gameCollection = gameCollection;
        IsDiscordIntegrationGloballyDisabled = ClientConfiguration.Instance.DiscordIntegrationGloballyDisabled;
    }

    // --- Commands ---

    [RelayCommand]
    private void LoadSettings()
    {
        PingUnofficialTunnels = iniSettings.PingUnofficialCnCNetTunnels;
        WriteInstallationPathToRegistry = iniSettings.WritePathToRegistry;
        NotifyOnUserListChanges = iniSettings.NotifyOnUserListChange;
        DisablePrivateMessagePopups = iniSettings.DisablePrivateMessagePopups;
        DisableMainMenuHotkeys = iniSettings.DisableMainMenuHotkeys;
        AllowPrivateMessagesMode = iniSettings.AllowPrivateMessagesFromState;
        SelectedAllowPrivateMessagesFromIndex = EnumToIndex(AllowPrivateMessagesMode);
        AutoConnectOnStartup = iniSettings.AutomaticCnCNetLogin;
        SkipLoginDialog = iniSettings.SkipConnectDialog;
        PersistentMode = iniSettings.PersistentMode;
        IsSteamIntegrationEnabled = iniSettings.SteamIntegration;
        IsDiscordIntegrationEnabled = !ClientConfiguration.Instance.DiscordIntegrationGloballyDisabled
            && iniSettings.DiscordIntegration;
        AllowGameInvitesOnlyFromFriends = iniSettings.AllowGameInvitesFromFriendsOnly;

        // Update auto-connect allowance
        UpdateAutoConnectOnStartupAllowance();

        // Load followed games
        _followedGameNames.Clear();
        _gameListItems.Clear();
        string localGame = ClientConfiguration.Instance.LocalGame.ToUpperInvariant();
        foreach (var game in gameCollection.GameList.Where(g => g.Supported && !string.IsNullOrEmpty(g.GameBroadcastChannel)))
        {
            bool isLocalGame = game.InternalName.ToUpperInvariant() == localGame;
            bool isFollowed = isLocalGame || iniSettings.IsGameFollowed(game.InternalName);

            if (isFollowed)
                _followedGameNames.Add(game.InternalName);

            _gameListItems.Add(new GameListItemData(game.InternalName, game.UIName, isLocalGame, isFollowed));
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        iniSettings.PingUnofficialCnCNetTunnels.Value = PingUnofficialTunnels;
        iniSettings.WritePathToRegistry.Value = WriteInstallationPathToRegistry;
        iniSettings.NotifyOnUserListChange.Value = NotifyOnUserListChanges;
        iniSettings.DisablePrivateMessagePopups.Value = DisablePrivateMessagePopups;
        iniSettings.DisableMainMenuHotkeys.Value = DisableMainMenuHotkeys;
        iniSettings.AllowPrivateMessagesFromState.Value = PmModeByIndex[SelectedAllowPrivateMessagesFromIndex];
        iniSettings.AutomaticCnCNetLogin.Value = AutoConnectOnStartup;
        iniSettings.SkipConnectDialog.Value = SkipLoginDialog;
        iniSettings.PersistentMode.Value = PersistentMode;
        iniSettings.SteamIntegration.Value = IsSteamIntegrationEnabled;

        if (!ClientConfiguration.Instance.DiscordIntegrationGloballyDisabled)
            iniSettings.DiscordIntegration.Value = IsDiscordIntegrationEnabled;

        iniSettings.AllowGameInvitesFromFriendsOnly.Value = AllowGameInvitesOnlyFromFriends;

        // Save followed games
        string localGame = ClientConfiguration.Instance.LocalGame.ToUpperInvariant();
        foreach (var game in gameCollection.GameList.Where(g => g.Supported && !string.IsNullOrEmpty(g.GameBroadcastChannel)))
        {
            if (game.InternalName.ToUpperInvariant() == localGame)
                iniSettings.SettingsIni.SetBooleanValue("Channels", localGame, true);
            else
                iniSettings.SettingsIni.SetBooleanValue("Channels", game.InternalName.ToUpperInvariant(),
                    _followedGameNames.Contains(game.InternalName));
        }
    }

    [RelayCommand]
    private void ToggleGameFollowed(string gameInternalName)
    {
        string localGame = ClientConfiguration.Instance.LocalGame.ToUpperInvariant();
        if (gameInternalName.ToUpperInvariant() == localGame)
            return; // Can't unfollow local game

        if (_followedGameNames.Contains(gameInternalName))
            _followedGameNames.Remove(gameInternalName);
        else
            _followedGameNames.Add(gameInternalName);
    }

    // --- Property change handlers ---

    partial void OnSkipLoginDialogChanged(bool value) => UpdateAutoConnectOnStartupAllowance();
    partial void OnPersistentModeChanged(bool value) => UpdateAutoConnectOnStartupAllowance();

    // --- Helpers ---

    private void UpdateAutoConnectOnStartupAllowance()
    {
        IsAutoConnectOnStartupAllowed = SkipLoginDialog && PersistentMode;
        if (!IsAutoConnectOnStartupAllowed)
            AutoConnectOnStartup = false;
    }

    partial void OnSelectedAllowPrivateMessagesFromIndexChanged(int value)
    {
        if (value >= 0 && value < PmModeByIndex.Length)
            AllowPrivateMessagesMode = PmModeByIndex[value];
    }

    private int EnumToIndex(int enumValue)
    {
        int idx = Array.IndexOf(PmModeByIndex, enumValue);
        return idx >= 0 ? idx : 0; // Default to "All" if not found
    }
}

/// <summary>
/// Data for a game list item.
/// </summary>
public record GameListItemData(string InternalName, string UIName, bool IsLocalGame, bool IsFollowed) : IGameListItemData;


