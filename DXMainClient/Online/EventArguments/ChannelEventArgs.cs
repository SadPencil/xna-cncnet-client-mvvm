using System;

namespace DTAClient.Online.EventArguments
{
    public class ChannelEventArgs : EventArgs // checked
    {
        public ChannelEventArgs(string channelName)
        {
            ChannelName = channelName;
        }

        public string ChannelName { get; private set; }
    }
}
