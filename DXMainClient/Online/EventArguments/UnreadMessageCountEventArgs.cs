using System;

namespace DTAClient.Online.EventArguments
{
    public class UnreadMessageCountEventArgs : EventArgs // checked
    {
        public int UnreadMessageCount { get; set; }

        public UnreadMessageCountEventArgs(int unreadMessageCount)
        {
            UnreadMessageCount = unreadMessageCount;
        }
    }
}
