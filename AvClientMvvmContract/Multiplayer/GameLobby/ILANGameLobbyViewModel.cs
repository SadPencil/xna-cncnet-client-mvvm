using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

public interface ILANGameLobbyViewModel : IMultiplayerGameLobbyViewModel
{
    string LocalAddressText { get; }
    int ChatColorIndex { get; set; }

    IRelayCommand BroadcastGameStateCommand { get; }
}
