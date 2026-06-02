using System;

using AvMainClientViewModel.Online;

namespace AvMainClientViewModel.Multiplayer.CnCNet;

public class RecentPlayerTableRightClickEventArgs : EventArgs
{
    public IRCUser IrcUser { get; }

    public RecentPlayerTableRightClickEventArgs(IRCUser ircUser)
    {
        IrcUser = ircUser;
    }
}
