using System.Net;
using System.Net.Sockets;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Multiplayer.GameLobby;

public interface ILANGameLobbyViewModel : IMultiplayerGameLobbyViewModel
{
    string LocalAddressText { get; }
    int ChatColorIndex { get; set; }

    IRelayCommand BroadcastGameStateCommand { get; }
}
