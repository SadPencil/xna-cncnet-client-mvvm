using System;

namespace AvClientMvvmContract.Online;

/// <summary>
/// Read-only view of a chat message.
/// </summary>
public interface IChatMessage
{
    string SenderName { get; }
    string SenderIdent { get; }
    IRgb24Color Color { get; }
    DateTime DateTime { get; }
    string Message { get; }
    bool SenderIsAdmin { get; }
    bool IsUser { get; }

    /// <summary>Formatted as "[HH:mm] SenderName: Message" or "[HH:mm] Message" (system).</summary>
    string FormattedText { get; }
}
