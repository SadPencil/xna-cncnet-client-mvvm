using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using DXMainClientViewModel.Domain.Multiplayer;
using DXMainClientViewModel.Domain.Multiplayer.CnCNet;
using DXMainClientViewModel.Online;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

public interface ICnCNetGameLobbyViewModel : IMultiplayerGameLobbyViewModel
{
    string ChannelName { get; }
    string SelectedTunnelName { get; }
    bool IsEnabled { get; }

    IRelayCommand ChangeTunnelCommand { get; }
    IRelayCommand InvitePlayerCommand { get; }

    void SetUp(Channel channel, bool isHost, int maxPlayers, CnCNetTunnel tunnel, string hostName, bool isCustomPassword, int skillLevel);
    void OnJoined();
    void Clear();
    void LeaveGameLobby();
    void AddWarning(string warning);
    List<IGameSessionSetting> GetBroadcastableSettings();
    int GetBroadcastableCheckboxCount();
    int GetBroadcastableDropdownCount();
}
