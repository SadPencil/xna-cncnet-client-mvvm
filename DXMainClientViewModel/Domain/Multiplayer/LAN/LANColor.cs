namespace DXMainClientViewModel.Domain.Multiplayer.LAN
{
    public class LANColor
    {
        public LANColor(string name, int r, int g, int b)
        {
            Name = name;
            R = r;
            G = g;
            B = b;
        }

        public string Name { get; private set; }
        public int R { get; private set; }
        public int G { get; private set; }
        public int B { get; private set; }
    }
}
