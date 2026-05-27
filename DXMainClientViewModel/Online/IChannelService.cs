#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DXMainClientViewModel.Online;

public interface IChannelService
{
    string Name { get; }
    string DisplayName { get; }
    string Topic { get; }
    IReadOnlyList<string> UserNames { get; }
    IReadOnlyList<string> Messages { get; }

    Task SendMessageAsync(string message);
    void AddUser(string userName);
    void RemoveUser(string userName);
    void ClearUsers();
}
