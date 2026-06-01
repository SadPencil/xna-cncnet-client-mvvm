using System.Net;
using System.Net.Sockets;

namespace DXMainClientMvvmContract.Multiplayer;

public interface ILANGameLoadingLobbyViewModel : IGameLoadingLobbyViewModel
{
    string LocalAddressText { get; }
    bool AreAllPlayersReady { get; }
    bool IsEnabled { get; }
}
