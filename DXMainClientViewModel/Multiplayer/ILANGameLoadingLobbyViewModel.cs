using System.Net;
using System.Net.Sockets;

namespace DXMainClientViewModel.Multiplayer;

public interface ILANGameLoadingLobbyViewModel : IGameLoadingLobbyViewModel
{
    string LocalAddressText { get; }
    bool AreAllPlayersReady { get; }
    bool IsEnabled { get; }

    // --- Lifecycle ---
    void SetUp(bool isHost, IPEndPoint hostEndPoint, TcpClient client, int loadedGameId);
    void PostJoin();
    void SetChatColorIndex(int colorIndex);
}
