namespace DXMainClientViewModel.Domain;

public interface IDiscordHandlerService
{
    bool IsEnabled { get; }
    string CurrentStatusText { get; }

    void Connect();
    void Disconnect();
    void SetMainMenuPresence();
    void SetCampaignPresence(string missionName, string difficultyName, string side, bool resetTimer);
    void SetSkirmishPresence(string mapName, string gameModeName);
    void SetMultiplayerPresence(string roomName, string mapName, string gameModeName, int currentPlayers, int maxPlayers);
    void ClearPresence();
}
