#nullable enable

using System.Collections.Generic;

namespace DXMainClientView;

public interface ITunnelListBoxView
{
    void SetTunnels(IEnumerable<IReadOnlyList<string>> tunnels);
    void SetSelectedTunnelAddress(string tunnelAddress);
    void SetTunnelPing(string tunnelAddress, int ping);
    void SortByPing();
    void RefreshList();
    void ClearTunnels();
}
