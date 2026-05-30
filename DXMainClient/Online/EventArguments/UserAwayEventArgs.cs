using System;

namespace DTAClient.Online.EventArguments
{
    public class UserAwayEventArgs : EventArgs // checked
    {
        public UserAwayEventArgs(string user, string awayReason)
        {
            UserName = user;
            AwayReason = awayReason;
        }

        public string UserName { get; private set; }

        public string AwayReason { get; private set; }
    }
}
