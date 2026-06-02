using AvMainClientMvvmContract;

namespace AvMainClientViewModel.Domain.Multiplayer.LAN
{
    public class LANColor
    {
        public LANColor(string name, IRgb24Color color)
        {
            Name = name;
            Color = color;
        }

        public string Name { get; private set; }
        public IRgb24Color Color { get; private set; }
    }
}
