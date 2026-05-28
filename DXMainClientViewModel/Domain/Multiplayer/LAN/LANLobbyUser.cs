using System;
using System.Net;

using SixLabors.ImageSharp;

namespace DXMainClientViewModel.Domain.Multiplayer.LAN
{
    public class LANLobbyUser
    {
        public LANLobbyUser(string name, Image gameImage, IPEndPoint endPoint)
        {
            Name = name;
            GameImage = gameImage;
            EndPoint = endPoint;
        }

        public string Name { get; private set; }
        public Image GameImage { get; private set; }
        public IPEndPoint EndPoint { get; private set; }

        private readonly object timeWithoutRefreshLock = new();
        public TimeSpan TimeWithoutRefresh { get; private set; }

        public void ClearTimeWithoutRefresh()
        {
            lock (timeWithoutRefreshLock)
            {
                TimeWithoutRefresh = TimeSpan.Zero;
            }
        }

        public void AddToTimeWithoutRefresh(TimeSpan timeToAdd)
        {
            lock (timeWithoutRefreshLock)
            {
                TimeWithoutRefresh += timeToAdd;
            }
        }
    }
}
