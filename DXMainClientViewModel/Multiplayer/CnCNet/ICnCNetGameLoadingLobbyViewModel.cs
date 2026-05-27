#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface ICnCNetGameLoadingLobbyViewModel : IGameLoadingLobbyViewModel
{
    string ChannelName { get; }
    string SelectedTunnelName { get; }

    IRelayCommand ChangeTunnelCommand { get; }
}
