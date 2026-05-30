using System;

using DXMainClientViewModel.Online;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

public class RecentPlayerTableRightClickEventArgs : EventArgs
{
    public IRCUser IrcUser { get; }

    public RecentPlayerTableRightClickEventArgs(IRCUser ircUser)
    {
        IrcUser = ircUser;
    }
}
