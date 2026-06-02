using System;

namespace AvMainClientViewModel.Online.EventArguments
{
    public class ChannelEventArgs : EventArgs
    {
        public ChannelEventArgs(string channelName)
        {
            ChannelName = channelName;
        }

        public string ChannelName { get; private set; }
    }
}
