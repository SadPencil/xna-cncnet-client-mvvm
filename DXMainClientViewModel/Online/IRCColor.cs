namespace DXMainClientViewModel.Online
{
    public class IRCColor
    {
        public IRCColor(string name, bool selectable, int r, int g, int b, int ircColorId)
        {
            Name = name;
            Selectable = selectable;
            R = r;
            G = g;
            B = b;
            IrcColorId = ircColorId;
        }

        public string Name { get; private set; }
        public bool Selectable { get; private set; }
        public int R { get; private set; }
        public int G { get; private set; }
        public int B { get; private set; }
        public int IrcColorId { get; private set; }
    }
}
