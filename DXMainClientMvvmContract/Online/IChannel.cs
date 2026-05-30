namespace DXMainClientMvvmContract.Online;

/// <summary>
/// Read-only view of a chat channel.
/// </summary>
public interface IChannel
{
    string ChannelName { get; }
    string Topic { get; }
    bool IsChatChannel { get; }
}
