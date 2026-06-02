using System;

namespace AvMainClientViewModel.Domain.Multiplayer.CnCNet;

public class TunnelEventArgs : EventArgs
{
    public TunnelEventArgs(CnCNetTunnel tunnel)
    {
        Tunnel = tunnel;
    }

    public CnCNetTunnel Tunnel { get; }
}
