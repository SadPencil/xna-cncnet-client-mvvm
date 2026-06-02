using System;

namespace AvClientViewModel.Online.EventArguments
{
    public class JoinUserEventArgs : EventArgs
    {
        public IRCUser IrcUser { get; }

        public JoinUserEventArgs(IRCUser ircUser)
        {
            IrcUser = ircUser;
        }
    }
}
