using System;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    public class MapEventArgs : EventArgs // checked
    {
        public MapEventArgs(Map map)
        {
            Map = map;
        }

        public Map Map { get; private set; }
    }
}
