using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

public interface ICnCNetGameLobbyViewModel : IMultiplayerGameLobbyViewModel
{
    string ChannelName { get; }
    string SelectedTunnelName { get; }

    IRelayCommand ChangeTunnelCommand { get; }
    IRelayCommand InvitePlayerCommand { get; }
    IRelayCommand SaveGameOptionPresetCommand { get; }
    IRelayCommand LoadGameOptionPresetCommand { get; }
}
