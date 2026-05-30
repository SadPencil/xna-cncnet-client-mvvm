namespace DTAClient.Online.EventArguments
{
    public class PrivateMessageEventArgs : CnCNetPrivateMessageEventArgs // checked
    {
        public readonly IRCUser ircUser;

        public PrivateMessageEventArgs(string sender, string message, IRCUser ircUser) : base(sender, message)
        {
            this.ircUser = ircUser;
        }
    }
}
