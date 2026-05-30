using System;

using DXMainClientViewModel.Domain.Multiplayer;

namespace DXMainClientViewModel.Online.EventArguments
{
    public class FavoriteMapEventArgs : EventArgs
    {
        public readonly Map Map;

        public FavoriteMapEventArgs(Map map)
        {
            Map = map;
        }
    }
}
