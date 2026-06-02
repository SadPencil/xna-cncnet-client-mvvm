using System;
using System.Net;

namespace AvMainClientViewModel.LAN;

/// <summary>
/// Service interface for LAN lobby UDP broadcast management.
/// Handles socket management, broadcast interface discovery, and message sending/listening.
/// </summary>
public interface ILANBroadcastManagerService : IDisposable
{
    bool IsInitialized { get; }
    int BroadcastInterfaceCount { get; }

    event EventHandler<LANBroadcastMessageReceivedEventArgs> MessageReceived;

    void Initialize();
    bool SendMessage(string message);
    void Shutdown();
}

public class LANBroadcastMessageReceivedEventArgs : EventArgs
{
    public string Data { get; }
    public IPEndPoint EndPoint { get; }

    public LANBroadcastMessageReceivedEventArgs(string data, IPEndPoint endPoint)
    {
        Data = data;
        EndPoint = endPoint;
    }
}
