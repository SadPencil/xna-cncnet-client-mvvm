using System.Collections.Generic;

using AvClientMvvmContract.Domain.Multiplayer;
using AvClientMvvmContract.Online;
using AvClientMvvmContract.ViewServices;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

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

    string SelectedContextPlayerName { get; set; }

    // --- Commands ---
    IRelayCommand ChangeTunnelCommand { get; }
    IRelayCommand LeaveGameLobbyCommand { get; }
    IRelayCommand OpenContextPlayerPrivateMessageCommand { get; }
    IRelayCommand ToggleContextPlayerFriendCommand { get; }
    IRelayCommand ToggleContextPlayerIgnoreCommand { get; }

    IReadOnlyList<IContextMenuItem> PlayerContextMenuItems { get; }
}
