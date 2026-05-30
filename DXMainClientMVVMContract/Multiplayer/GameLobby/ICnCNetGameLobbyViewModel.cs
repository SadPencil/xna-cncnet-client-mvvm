using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using DXMainClientViewModel.Domain.Multiplayer;
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
    IIRCColor ChatColor { get; set; }

    // --- Commands ---
    IRelayCommand ChangeTunnelCommand { get; }
    IRelayCommand LeaveGameLobbyCommand { get; }

    // --- Lifecycle (called by main lobby when creating/joining game) ---
    void SetUp(IChannel channel, bool isHost, int maxPlayers, ICnCNetTunnel tunnel, string hostName, bool isCustomPassword, int skillLevel);
    void OnJoined();
    void Clear();
    void LeaveGameLobby();

    // --- Game option broadcasting ---
    List<IGameSessionSetting> GetBroadcastableSettings();
    int GetBroadcastableCheckboxCount();
    int GetBroadcastableDropdownCount();
}
