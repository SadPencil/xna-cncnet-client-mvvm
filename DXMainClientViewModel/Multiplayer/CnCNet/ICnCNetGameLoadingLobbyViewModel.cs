using CommunityToolkit.Mvvm.Input;
using DXMainClientViewModel.Domain.Multiplayer.CnCNet;
using DXMainClientViewModel.Online;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

public interface ICnCNetGameLoadingLobbyViewModel : IGameLoadingLobbyViewModel
{
    string ChannelName { get; }
    string SelectedTunnelName { get; }
    bool IsEnabled { get; }

    IRelayCommand ChangeTunnelCommand { get; }

    void SetUp(bool isHost, CnCNetTunnel tunnel, Channel channel, string hostName);
    void OnJoined();
    void Clear();
}
