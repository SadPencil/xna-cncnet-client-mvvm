using System;

namespace DXMainClientViewModel.Domain.Multiplayer;

public class GameLoadEventArgs : EventArgs
{
    public GameLoadEventArgs(int loadedGameId)
    {
        LoadedGameID = loadedGameId;
    }

    public int LoadedGameID { get; private set; }
}
