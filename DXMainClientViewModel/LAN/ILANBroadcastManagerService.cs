#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DXMainClientViewModel;

public interface ILANBroadcastManagerService
{
    bool IsRunning { get; }
    IReadOnlyList<string> BroadcastAddresses { get; }

    void Start();
    void Stop();
    Task SendBroadcastAsync(string message);
    Task<IReadOnlyList<string>> ReceivePendingMessagesAsync();
}
