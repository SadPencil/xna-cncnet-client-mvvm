using System.Collections.Generic;
using System.Threading.Tasks;

namespace AvMainClientViewModel.Domain.Multiplayer.CnCNet;

public interface ITunnelHandlerService
{
    IReadOnlyList<string> TunnelNames { get; }
    IReadOnlyList<string> TunnelAddresses { get; }
    int SelectedTunnelIndex { get; set; }

    Task RefreshTunnelsAsync();
    Task<int> PingTunnelAsync(string tunnelAddress);
    void SelectTunnel(string tunnelAddress);
}
