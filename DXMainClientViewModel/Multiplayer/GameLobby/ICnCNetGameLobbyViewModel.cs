using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using DXMainClientViewModel.Domain.Multiplayer;
using DXMainClientViewModel.Domain.Multiplayer.CnCNet;
using DXMainClientViewModel.Online;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

public interface ICnCNetGameLobbyViewModel : IMultiplayerGameLobbyViewModel
{
    // --- Observable state ---
    string ChannelName { get; }
    string SelectedTunnelName { get; }
    bool IsEnabled { get; }
    string GameRoomName { get; }
    int PlayerLimit { get; }
    int SkillLevel { get; }
    bool IsCustomPassword { get; }
    bool TunnelErrorMode { get; }

    // --- Commands ---
    IRelayCommand ChangeTunnelCommand { get; }
    IRelayCommand InvitePlayerCommand { get; }
    IRelayCommand LeaveGameLobbyCommand { get; }

    // --- Lifecycle (called by main lobby when creating/joining game) ---
    void SetUp(Channel channel, bool isHost, int maxPlayers, CnCNetTunnel tunnel, string hostName, bool isCustomPassword, int skillLevel);
    void OnJoined();
    void Clear();
    void LeaveGameLobby();

    // --- Game option broadcasting ---
    List<IGameSessionSetting> GetBroadcastableSettings();
    int GetBroadcastableCheckboxCount();
    int GetBroadcastableDropdownCount();

    // --- Events ---
    event EventHandler GameLeft;
    event EventHandler<string> TunnelSelectionRequested;
    event EventHandler<string> GameLobbySettingsRequested;
}
