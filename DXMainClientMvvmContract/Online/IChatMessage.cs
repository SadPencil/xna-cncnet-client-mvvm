using System;

namespace DXMainClientMVVMContract.Online;

/// <summary>
/// Read-only view of a chat message.
/// </summary>
public interface IChatMessage
{
    string SenderName { get; }
    string SenderIdent { get; }
    int R { get; }
    int G { get; }
    int B { get; }
    DateTime DateTime { get; }
    string Message { get; }
    bool SenderIsAdmin { get; }
    bool IsUser { get; }
}
