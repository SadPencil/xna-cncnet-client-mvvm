namespace DXMainClientViewModel.Domain.Multiplayer.CnCNet;

public interface IGameBroadcastService
{
    bool IsBroadcasting { get; }
    string HostedGameName { get; }

    void StartBroadcasting(string gameName, string mapName, string gameModeName, int maxPlayers, bool isPasswordProtected);
    void UpdateBroadcast(string gameName, string mapName, string gameModeName, int currentPlayers, int maxPlayers);
    void StopBroadcasting();
}
