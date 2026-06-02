namespace AvMainClientMvvmContract.Multiplayer.CnCNet;

/// <summary>
/// Read-only view of data for a pending game invite notification.
/// </summary>
public interface IPendingGameInviteData
{
    string Sender { get; }
    string GameName { get; }
    string ChannelName { get; }
    string Password { get; }
}
