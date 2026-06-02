using CommunityToolkit.Mvvm.Input;

using AvClientMvvmContract.Domain.Multiplayer;
using AvClientMvvmContract.Online;

namespace AvClientMvvmContract.Multiplayer.CnCNet;

public interface ICnCNetGameLoadingLobbyViewModel : IGameLoadingLobbyViewModel
{
    string ChannelName { get; }
    string SelectedTunnelName { get; }
    bool IsEnabled { get; }
    bool IsChangeTunnelVisible { get; }
    int ChatColorIndex { get; }

    IRelayCommand ChangeTunnelCommand { get; }
}
