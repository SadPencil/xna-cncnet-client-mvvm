using System.Collections.Generic;
using System.Threading.Tasks;

namespace AvMainClientViewModel.Online;

public interface IConnectionManagerService
{
    bool IsConnected { get; }
    bool IsAttemptingConnection { get; }
    string? CurrentServerName { get; }
    string? MainChannelName { get; }
    IReadOnlyList<string> JoinedChannelNames { get; }

    Task ConnectAsync();
    Task DisconnectAsync();
    Task JoinChannelAsync(string channelName, string? password = null);
    Task LeaveChannelAsync(string channelName);
    Task SendRawMessageAsync(string message);
}
