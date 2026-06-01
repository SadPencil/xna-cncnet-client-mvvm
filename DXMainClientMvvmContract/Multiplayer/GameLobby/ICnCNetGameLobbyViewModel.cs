using System.Collections.Generic;

using CommunityToolkit.Mvvm.Input;

using DXMainClientMvvmContract.Domain.Multiplayer;
using DXMainClientMvvmContract.Online;

namespace DXMainClientMvvmContract.Multiplayer.GameLobby;

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
}
