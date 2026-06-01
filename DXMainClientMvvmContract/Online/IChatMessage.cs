using System;

namespace DXMainClientMvvmContract.Online;

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
}
