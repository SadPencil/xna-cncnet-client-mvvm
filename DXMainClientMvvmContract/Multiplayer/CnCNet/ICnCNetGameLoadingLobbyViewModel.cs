using CommunityToolkit.Mvvm.Input;

using DXMainClientMvvmContract.Domain.Multiplayer;
using DXMainClientMvvmContract.Online;

namespace DXMainClientMvvmContract.Multiplayer.CnCNet;

public interface ICnCNetGameLoadingLobbyViewModel : IGameLoadingLobbyViewModel
{
    string ChannelName { get; }
    string SelectedTunnelName { get; }
    bool IsEnabled { get; }
    bool IsChangeTunnelVisible { get; }
    int ChatColorIndex { get; }

    IRelayCommand ChangeTunnelCommand { get; }
}
