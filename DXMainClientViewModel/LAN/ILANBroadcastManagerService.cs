using System.Collections.Generic;
using System.Threading.Tasks;

namespace DXMainClientViewModel.LAN;

public interface ILANBroadcastManagerService
{
    bool IsRunning { get; }
    IReadOnlyList<string> BroadcastAddresses { get; }

    void Start();
    void Stop();
    Task SendBroadcastAsync(string message);
    Task<IReadOnlyList<string>> ReceivePendingMessagesAsync();
}
