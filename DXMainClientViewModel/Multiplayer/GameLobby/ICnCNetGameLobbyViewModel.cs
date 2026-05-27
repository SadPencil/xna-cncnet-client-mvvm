#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface ICnCNetGameLobbyViewModel : IMultiplayerGameLobbyViewModel
{
    string ChannelName { get; }
    string SelectedTunnelName { get; }

    IRelayCommand ChangeTunnelCommand { get; }
    IRelayCommand InvitePlayerCommand { get; }
    IRelayCommand SaveGameOptionPresetCommand { get; }
    IRelayCommand LoadGameOptionPresetCommand { get; }
}
