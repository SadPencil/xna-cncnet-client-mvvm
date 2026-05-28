using System;
using System.Net;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

public interface ILANGameLobbyViewModel : IMultiplayerGameLobbyViewModel
{
    string LocalAddressText { get; }

    IRelayCommand BroadcastGameStateCommand { get; }

    // --- Lifecycle ---
    void SetUp(bool isHost, IPEndPoint hostEndPoint, TcpClient client);
    void PostJoin();
    void SetChatColorIndex(int colorIndex);

    // --- Events ---
    event EventHandler<LobbyNotificationEventArgs> LobbyNotification;
    event EventHandler<GameLeftEventArgs> GameLeft;
    event EventHandler<GameBroadcastEventArgs> GameBroadcast;
    event EventHandler JoinSoundRequested;
    event EventHandler LeaveSoundRequested;
    event EventHandler ReturnSoundRequested;
}
