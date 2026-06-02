using CommunityToolkit.Mvvm.Input;

namespace AvMainClientMvvmContract.Multiplayer.GameLobby;

public interface ILANGameLobbyViewModel : IMultiplayerGameLobbyViewModel
{
    string LocalAddressText { get; }
    int ChatColorIndex { get; set; }

    IRelayCommand BroadcastGameStateCommand { get; }
}
