using AvClientMvvmContract.Online;

using Serilog;

namespace AvClientViewModel.Online
{
    public class IRCColor : IIRCColor
    {
        public IRCColor(string name, bool selectable, byte r, byte g, byte b, int ircColorId)
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
        public byte R { get; private set; }
        public byte G { get; private set; }
        public byte B { get; private set; }
        public int IrcColorId { get; private set; }

        public override string ToString()
        {
            var result = Name;
            Log.Debug("[LOG-IRCColor] ToString() called, returning '{Name}'", result);
            return result;
        }
    }
}
