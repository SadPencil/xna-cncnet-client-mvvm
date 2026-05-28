using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer.CnCNet;

namespace DXMainClientViewModel.Generic.OptionPanels;

/// <summary>
/// ViewModel for the CnCNet options panel.
/// Contains all business logic from CnCNetOptionsPanel.cs except XNA UI rendering.
/// </summary>
public partial class CnCNetOptionsPanelViewModel : ObservableObject, ICnCNetOptionsPanelViewModel
{
    private readonly UserINISettings iniSettings;
    private readonly GameCollection gameCollection;

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

    // --- Observable collections ---

    private readonly ObservableCollection<string> _followedGameNames = new();
    public IReadOnlyList<string> FollowedGameNames => _followedGameNames;

    // --- Constructor ---

    public CnCNetOptionsPanelViewModel(UserINISettings iniSettings, GameCollection gameCollection)
    {
        this.iniSettings = iniSettings;
        this.gameCollection = gameCollection;
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
        AutoConnectOnStartup = iniSettings.AutomaticCnCNetLogin;
        SkipLoginDialog = iniSettings.SkipConnectDialog;
        PersistentMode = iniSettings.PersistentMode;
        IsSteamIntegrationEnabled = iniSettings.SteamIntegration;
        IsDiscordIntegrationEnabled = !ClientConfiguration.Instance.DiscordIntegrationGloballyDisabled
            && iniSettings.DiscordIntegration;
        AllowGameInvitesOnlyFromFriends = iniSettings.AllowGameInvitesFromFriendsOnly;

        // Load followed games
        _followedGameNames.Clear();
        string localGame = ClientConfiguration.Instance.LocalGame.ToUpperInvariant();
        foreach (var game in gameCollection.GameList.Where(g => g.Supported && !string.IsNullOrEmpty(g.GameBroadcastChannel)))
        {
            if (game.InternalName.ToUpperInvariant() == localGame)
                _followedGameNames.Add(game.InternalName); // Always followed
            else if (iniSettings.IsGameFollowed(game.InternalName))
                _followedGameNames.Add(game.InternalName);
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
        iniSettings.AllowPrivateMessagesFromState.Value = AllowPrivateMessagesMode;
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

    // --- Property change handlers ---

    partial void OnSkipLoginDialogChanged(bool value) => CheckConnectOnStartupAllowance();
    partial void OnPersistentModeChanged(bool value) => CheckConnectOnStartupAllowance();

    // --- Helpers ---

    private void CheckConnectOnStartupAllowance()
    {
        if (!SkipLoginDialog || !PersistentMode)
            AutoConnectOnStartup = false;
    }
}
