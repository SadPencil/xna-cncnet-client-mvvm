using CommunityToolkit.Mvvm.Input;
using DXMainClientViewModel.Domain.Multiplayer;
using DXMainClientViewModel.Online;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

public interface ICnCNetGameLoadingLobbyViewModel : IGameLoadingLobbyViewModel
{
    string ChannelName { get; }
    string SelectedTunnelName { get; }
    bool IsEnabled { get; }
    bool IsChangeTunnelVisible { get; }
    int ChatColorIndex { get; }

    IRelayCommand ChangeTunnelCommand { get; }

    void SetUp(bool isHost, ICnCNetTunnel tunnel, IChannel channel, string hostName);
    void OnJoined();
    void Clear();
    void ChangeChatColor(IIRCColor color);
    void OnTunnelSelected(ICnCNetTunnel tunnel);
}
