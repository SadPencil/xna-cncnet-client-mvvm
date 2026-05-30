using System.Net;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMVVMContract.Multiplayer.GameLobby;

public interface ILANGameLobbyViewModel : IMultiplayerGameLobbyViewModel
{
    string LocalAddressText { get; }
    int ChatColorIndex { get; set; }

    IRelayCommand BroadcastGameStateCommand { get; }

    // --- Lifecycle ---
    void SetUp(bool isHost, IPEndPoint hostEndPoint, TcpClient client);
    void PostJoin();
}
