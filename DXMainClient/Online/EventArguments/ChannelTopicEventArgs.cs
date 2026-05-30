using System;

namespace DTAClient.Online.EventArguments
{
    public class ChannelTopicEventArgs : EventArgs // checked
    {
        public ChannelTopicEventArgs(string channelName, string topic)
        {
            ChannelName = channelName;
            Topic = topic;
        }

        public string ChannelName { get; private set; }
        public string Topic { get; private set; }
    }
}
