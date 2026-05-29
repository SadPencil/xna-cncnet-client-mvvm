using DXMainClientViewModel.Domain;

namespace DXMainClientView.Services;

/// <summary>
/// Stub implementation of IDiscordHandlerService for the View layer.
/// No Discord integration in Avalonia view-only mode.
/// </summary>
public class StubDiscordHandlerService : IDiscordHandlerService
{
    public bool IsEnabled => false;
    public string CurrentStatusText => string.Empty;

    public void Connect() { }
    public void Disconnect() { }
    public void SetMainMenuPresence() { }
    public void SetCampaignPresence(string missionName, string difficultyName, string side, bool resetTimer) { }
    public void SetSkirmishPresence(string mapName, string gameModeName) { }
    public void SetMultiplayerPresence(string roomName, string mapName, string gameModeName, int currentPlayers, int maxPlayers) { }
    public void ClearPresence() { }
    public void SetSavedGamePresence(string saveName, bool resetTimer) { }
}
