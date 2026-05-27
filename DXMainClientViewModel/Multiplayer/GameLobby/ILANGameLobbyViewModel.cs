using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

public interface ILANGameLobbyViewModel : IMultiplayerGameLobbyViewModel
{
    string LocalAddressText { get; }

    IRelayCommand BroadcastGameStateCommand { get; }
}
