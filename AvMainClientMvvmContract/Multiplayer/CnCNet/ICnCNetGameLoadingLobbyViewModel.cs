using CommunityToolkit.Mvvm.Input;

using AvMainClientMvvmContract.Domain.Multiplayer;
using AvMainClientMvvmContract.Online;

namespace AvMainClientMvvmContract.Multiplayer.CnCNet;

public interface ICnCNetGameLoadingLobbyViewModel : IGameLoadingLobbyViewModel
{
    string ChannelName { get; }
    string SelectedTunnelName { get; }
    bool IsEnabled { get; }
    bool IsChangeTunnelVisible { get; }
    int ChatColorIndex { get; }

    IRelayCommand ChangeTunnelCommand { get; }
}
