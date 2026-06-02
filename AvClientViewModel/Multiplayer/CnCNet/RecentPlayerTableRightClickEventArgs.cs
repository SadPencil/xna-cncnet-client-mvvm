using System;

using AvClientViewModel.Online;

namespace AvClientViewModel.Multiplayer.CnCNet;

public class RecentPlayerTableRightClickEventArgs : EventArgs
{
    public IRCUser IrcUser { get; }

    public RecentPlayerTableRightClickEventArgs(IRCUser ircUser)
    {
        IrcUser = ircUser;
    }
}
