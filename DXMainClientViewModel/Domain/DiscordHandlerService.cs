using ClientCore;

namespace DXMainClientViewModel.Domain
{
    /// <summary>
    /// Adapter implementing IDiscordHandlerService by delegating to DiscordHandler.
    /// </summary>
    public class DiscordHandlerService : IDiscordHandlerService
    {
        private readonly DiscordHandler discordHandler;

        public bool IsEnabled =>
            UserINISettings.Instance.DiscordIntegration
            && !ClientConfiguration.Instance.DiscordIntegrationGloballyDisabled;

        public string CurrentStatusText => IsEnabled ? "Connected" : "Disabled";

        public DiscordHandlerService()
        {
            discordHandler = new DiscordHandler();
        }

        public void Connect() => discordHandler.Connect();

        public void Disconnect() => discordHandler.Disconnect();

        public void SetMainMenuPresence() => discordHandler.UpdatePresence();

        public void SetCampaignPresence(string missionName, string difficultyName, string side, bool resetTimer)
            => discordHandler.UpdatePresence(missionName, difficultyName, side, resetTimer);

        public void SetSkirmishPresence(string mapName, string gameModeName)
            => discordHandler.UpdatePresence(mapName, gameModeName, "Skirmish", "Playing", false);

        public void SetMultiplayerPresence(string roomName, string mapName, string gameModeName, int currentPlayers, int maxPlayers)
            => discordHandler.UpdatePresence(mapName, gameModeName, "Multiplayer", "In Lobby",
                currentPlayers, maxPlayers, "", roomName);

        public void ClearPresence() => discordHandler.UpdatePresence();

        public void SetSavedGamePresence(string saveName, bool resetTimer)
            => discordHandler.UpdatePresence(saveName, resetTimer);
    }
}
