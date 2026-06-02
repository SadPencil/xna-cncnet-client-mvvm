using System;

namespace AvMainClientViewModel.LAN;

/// <summary>
/// Service interface for LAN message deduplication.
/// Generates unique message IDs for outgoing messages and tracks received message IDs
/// to filter out duplicates.
/// </summary>
public interface ILANMessageDeduplicatorService : IDisposable
{
    int TrackedMessageCount { get; }

    string GenerateMessageId();
    string WrapMessage(string payload);
    void UnwrapMessage(string wrappedMessage, out string payload, out bool isDuplicate);
    void AddMessage(string messageId, out bool isDuplicate);
    void Clear();
}
