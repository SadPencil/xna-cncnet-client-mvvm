using ClientCore.Extensions;

namespace AvClientView.Localization;

/// <summary>
/// Contains compile-time .L10N() calls for every translation key used in AXAML files.
/// The <c>TranslationNotifierGenerator</c> Roslyn source generator scans this file at
/// compile time and generates <c>TranslationNotifier.Register()</c>, which is then
/// called at runtime via <c>ViewTranslationNotifierService</c> to register all View
/// translation keys for stub generation.
/// </summary>
internal static class ViewLocalizedStrings
{
    // ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
    internal static readonly string[] _keys =
#pragma warning restore IDE1006
    {
        // ==================================================================
        // Common Buttons
        // ==================================================================
        "Launch".L10N("Client:UI:ButtonLaunch"),
        "OK".L10N("Client:UI:ButtonOK"),
        "Cancel".L10N("Client:UI:ButtonCancel"),
        "Yes".L10N("Client:UI:ButtonYes"),
        "No".L10N("Client:UI:ButtonNo"),
        "Save".L10N("Client:UI:ButtonSave"),
        "Load".L10N("Client:UI:ButtonLoad"),
        "Delete".L10N("Client:UI:ButtonDelete"),
        "Download".L10N("Client:UI:ButtonDownload"),
        "Close".L10N("Client:UI:ButtonClose"),
        "Confirm".L10N("Client:UI:ButtonConfirm"),
        "Connect".L10N("Client:UI:ButtonConnect"),
        "Create".L10N("Client:UI:ButtonCreate"),
        "Install".L10N("Client:UI:ButtonInstall"),
        "Update".L10N("Client:UI:ButtonUpdate"),
        "Uninstall".L10N("Client:UI:ButtonUninstall"),
        "Send".L10N("Client:UI:ButtonSend"),
        "Accept".L10N("Client:UI:ButtonAccept"),
        "Decline".L10N("Client:UI:ButtonDecline"),
        "Open".L10N("Client:UI:ButtonOpen"),
        "Dismiss".L10N("Client:UI:ButtonDismiss"),
        "Force Update".L10N("Client:UI:ButtonForceUpdate"),
        "Move Up".L10N("Client:UI:ButtonMoveUp"),
        "Move Down".L10N("Client:UI:ButtonMoveDown"),
        "Return to Main Menu".L10N("Client:UI:ButtonReturnToMainMenu"),
        "Clear Statistics".L10N("Client:UI:ButtonClearStatistics"),
        "View Changelog".L10N("Client:UI:ButtonViewChangelog"),
        "View Downloads".L10N("Client:UI:ButtonViewDownloads"),
        "Main Menu".L10N("Client:UI:ButtonMainMenu"),
        "Leave Game".L10N("Client:UI:ButtonLeaveGame"),
        "Add AI".L10N("Client:UI:ButtonAddAI"),
        "Remove".L10N("Client:UI:ButtonRemove"),
        "Configure Hotkeys".L10N("Client:UI:ButtonConfigureHotkeys"),
        "Leave".L10N("Client:UI:ButtonLeave"),
        "Auto Ready".L10N("Client:UI:ButtonAutoReady"),
        "Change Tunnel".L10N("Client:UI:ButtonChangeTunnel"),
        "New Game".L10N("Client:UI:ButtonNewGame"),
        "Load Game".L10N("Client:UI:ButtonLoadGame"),
        "Create Game".L10N("Client:UI:ButtonCreateGame"),
        "Join Game".L10N("Client:UI:ButtonJoinGame"),

        // ==================================================================
        // Campaign Selector
        // ==================================================================
        "MISSIONS:".L10N("Client:UI:CampaignMissions"),
        "MISSION DESCRIPTION:".L10N("Client:UI:CampaignMissionDescription"),
        "DIFFICULTY LEVEL".L10N("Client:UI:CampaignDifficultyLevel"),
        "Campaigns".L10N("Client:UI:ButtonCampaigns"),

        // ==================================================================
        // Cheater Window
        // ==================================================================
        // (TitleText, MessageText are bound; only buttons here)

        // ==================================================================
        // Main Menu
        // ==================================================================
        "Players Online:".L10N("Client:UI:MainMenuPlayersOnline"),

        // ==================================================================
        // Top Bar
        // ==================================================================
        "CnCNet Lobby".L10N("Client:UI:TopBarCnCNetLobby"),
        "Private Messages".L10N("Client:UI:TopBarPrivateMessages"),
        "Options".L10N("Client:UI:TopBarOptions"),
        "Logout".L10N("Client:UI:TopBarLogout"),
        "0".L10N("Client:UI:UnreadBadgeDefault"),

        // ==================================================================
        // Options Window - Tab Buttons
        // ==================================================================
        "Display".L10N("Client:UI:TabDisplay"),
        "Audio".L10N("Client:UI:TabAudio"),
        "Game".L10N("Client:UI:TabGame"),
        "CnCNet".L10N("Client:UI:TabCnCNet"),
        "Updater".L10N("Client:UI:TabUpdater"),
        "Components".L10N("Client:UI:TabComponents"),

        // ==================================================================
        // Options Window - Display Panel
        // ==================================================================
        "In-game Resolution:".L10N("Client:UI:OptionIngameResolution"),
        "Detail Level:".L10N("Client:UI:OptionDetailLevel"),
        "Renderer:".L10N("Client:UI:OptionRenderer"),
        "Windowed Mode".L10N("Client:UI:OptionWindowedMode"),
        "Borderless Windowed Mode".L10N("Client:UI:OptionBorderlessWindowedMode"),
        "Back Buffer in Video Memory".L10N("Client:UI:OptionBackBufferInVideoMemory"),
        "Fullscreen Client".L10N("Client:UI:OptionFullscreenClient"),
        "Alt+Enter toggles fullscreen client.".L10N("Client:UI:OptionAltEnterFullscreenHint"),
        "Client Theme:".L10N("Client:UI:OptionClientTheme"),
        "Language:".L10N("Client:UI:OptionLanguage"),
        "Generate Translation Stub".L10N("Client:UI:OptionGenerateTranslationStub"),
        "Output only missing values in translation stub".L10N("Client:UI:OptionTranslationStubOnlyNewValues"),
        "Install Game Compatibility Fix".L10N("Client:UI:OptionInstallGameCompatibilityFix"),
        "Install Map Editor Compatibility Fix".L10N("Client:UI:OptionInstallMapEditorCompatibilityFix"),
        "A restart is required for some changes to take effect.".L10N("Client:UI:OptionRestartRequiredHint"),
        "If set, the game's back buffer will be stored in VRAM...".L10N("Client:UI:OptionBackBufferToolTip"),

        // ==================================================================
        // Options Window - Audio Panel
        // ==================================================================
        "Music Volume:".L10N("Client:UI:OptionMusicVolume"),
        "Sound Volume:".L10N("Client:UI:OptionSoundVolume"),
        "Voice Volume:".L10N("Client:UI:OptionVoiceVolume"),
        "Client Volume:".L10N("Client:UI:OptionClientVolume"),
        "Shuffle Music".L10N("Client:UI:OptionShuffleMusic"),
        "Main Menu Music".L10N("Client:UI:OptionMainMenuMusic"),
        "Don't Stop Music on Menu".L10N("Client:UI:OptionDontStopMusicOnMenu"),
        "Play Sound on Game Lobby Messages".L10N("Client:UI:OptionPlaySoundOnGameLobbyMessages"),
        "Play Sound When Game is Hosted".L10N("Client:UI:OptionPlaySoundWhenGameIsHosted"),

        // ==================================================================
        // Options Window - Game Panel
        // ==================================================================
        "Scroll Rate:".L10N("Client:UI:OptionScrollRate"),
        "Scroll Coasting".L10N("Client:UI:OptionScrollCoasting"),
        "Target Lines".L10N("Client:UI:OptionTargetLines"),
        "Tooltips".L10N("Client:UI:OptionTooltips"),
        "Show Hidden Objects".L10N("Client:UI:OptionShowHiddenObjects"),
        "Black Chat Background".L10N("Client:UI:OptionBlackChatBackground"),
        "Alt+Click to Undeploy".L10N("Client:UI:OptionAltClickToUndeploy"),
        "Player Name:".L10N("Client:UI:OptionPlayerName"),

        // ==================================================================
        // Options Window - CnCNet Panel
        // ==================================================================
        "Ping Unofficial CnCNet Tunnels".L10N("Client:UI:OptionPingUnofficialTunnels"),
        "Write Game Installation Path to Windows Registry".L10N("Client:UI:OptionWriteGamePathToRegistry"),
        "Disable Hotkeys in Main Menu Lobby".L10N("Client:UI:OptionDisableHotkeysInLobby"),
        "Show Notification on User List Change".L10N("Client:UI:OptionShowNotificationOnUserListChange"),
        "Disable Private Message Popup Notifications".L10N("Client:UI:OptionDisablePrivateMessagePopup"),
        "Skip Login Dialog".L10N("Client:UI:OptionSkipLoginDialog"),
        "Stay Connected Outside CnCNet Lobby".L10N("Client:UI:OptionStayConnectedOutsideLobby"),
        "Connect to CnCNet on Client Startup".L10N("Client:UI:OptionConnectOnStartup"),
        "Show Game Info on Discord Status".L10N("Client:UI:OptionShowGameInfoOnDiscord"),
        "Only Allow Game Invites from Friends".L10N("Client:UI:OptionOnlyAllowInvitesFromFriends"),
        "Publish Game to Steam Friends".L10N("Client:UI:OptionPublishGameToSteamFriends"),
        "Allow PMs From:".L10N("Client:UI:OptionAllowPMsFrom"),
        "Follow Games:".L10N("Client:UI:OptionFollowGames"),

        // ==================================================================
        // Options Window - Updater Panel
        // ==================================================================
        "Update Server Priority".L10N("Client:UI:OptionUpdateServerPriority"),
        "Drag servers to change priority. The client will try servers from top to bottom.".L10N("Client:UI:OptionDragServersHint"),
        "Check for updates automatically".L10N("Client:UI:OptionCheckForUpdatesAutomatically"),

        // ==================================================================
        // Options Window - Components Panel
        // ==================================================================
        "Optional Components".L10N("Client:UI:OptionOptionalComponents"),
        "Install or update optional game components below.".L10N("Client:UI:OptionComponentsHint"),

        // ==================================================================
        // Statistics Window
        // ==================================================================
        "Game Statistics".L10N("Client:UI:BtnGameStatistics"),
        "Total Statistics".L10N("Client:UI:BtnTotalStatistics"),
        "FILTER:".L10N("Client:UI:StatsFilter"),
        "GAME MODE:".L10N("Client:UI:StatsGameMode"),
        "GAMES:".L10N("Client:UI:StatsGames"),
        "GAME DETAILS:".L10N("Client:UI:StatsGameDetails"),
        "Include spectated games".L10N("Client:UI:StatsIncludeSpectatedGames"),
        "Games started:".L10N("Client:UI:StatsGamesStarted"),
        "Games finished:".L10N("Client:UI:StatsGamesFinished"),
        "Wins:".L10N("Client:UI:StatsWins"),
        "Losses:".L10N("Client:UI:StatsLosses"),
        "Win/Loss ratio:".L10N("Client:UI:StatsWinLossRatio"),
        "Average game length:".L10N("Client:UI:StatsAvgGameLength"),
        "Total time played:".L10N("Client:UI:StatsTotalTimePlayed"),
        "Average enemies:".L10N("Client:UI:StatsAvgEnemies"),
        "Average allies:".L10N("Client:UI:StatsAvgAllies"),
        "Total kills:".L10N("Client:UI:StatsTotalKills"),
        "Kills per game:".L10N("Client:UI:StatsKillsPerGame"),
        "Total losses:".L10N("Client:UI:StatsTotalLosses"),
        "Losses per game:".L10N("Client:UI:StatsLossesPerGame"),
        "Kill/Loss ratio:".L10N("Client:UI:StatsKillLossRatio"),
        "Total score:".L10N("Client:UI:StatsTotalScore"),
        "Average economy:".L10N("Client:UI:StatsAvgEconomy"),
        "Favourite side:".L10N("Client:UI:StatsFavouriteSide"),
        "Average AI level:".L10N("Client:UI:StatsAvgAILevel"),
        "Clear Statistics".L10N("Client:UI:StatsClearStatisticsTitle"),
        "Are you sure you want to clear all statistics? This action cannot be undone.".L10N("Client:UI:StatsClearStatisticsConfirm"),

        // ==================================================================
        // Update Window
        // ==================================================================
        "Progress percentage of current file:".L10N("Client:UI:UpdateCurrentFileProgress"),
        "Total progress percentage:".L10N("Client:UI:UpdateTotalProgress"),

        // ==================================================================
        // Game In Progress Window
        // ==================================================================
        "A game is in progress.".L10N("Client:UI:GameInProgressText"),

        // ==================================================================
        // Game Loading Window
        // ==================================================================
        "Delete Confirmation".L10N("Client:UI:DeleteConfirmationTitle"),

        // ==================================================================
        // Extras Window
        // ==================================================================
        "Statistics".L10N("Client:UI:ExtrasStatistics"),
        "Map Editor".L10N("Client:UI:ExtrasMapEditor"),
        "Credits".L10N("Client:UI:ExtrasCredits"),

        // ==================================================================
        // Privacy Notification
        // ==================================================================
        "More information: ".L10N("Client:UI:PrivacyMoreInfo"),
        "|".L10N("Client:UI:PrivacyUrlSeparator"),

        // ==================================================================
        // Game Information Panel
        // ==================================================================
        "GAME INFORMATION".L10N("Client:UI:GameInfoHeader"),
        "Locked".L10N("Client:UI:GameInfoLocked"),
        "Passworded".L10N("Client:UI:GameInfoPassworded"),
        "Incompatible".L10N("Client:UI:GameInfoIncompatible"),
        "Host: ".L10N("Client:UI:GameInfoHost"),
        "Ping: ".L10N("Client:UI:GameInfoPing"),
        "Game version: ".L10N("Client:UI:GameInfoGameVersion"),
        "Game mode: ".L10N("Client:UI:GameInfoGameMode"),
        "Map: ".L10N("Client:UI:GameInfoMap"),
        "Skill: ".L10N("Client:UI:GameInfoSkill"),
        "Players: ".L10N("Client:UI:GameInfoPlayers"),
        " ms".L10N("Client:UI:GameInfoPingMs"),
        "Players:".L10N("Client:UI:GameInfoPlayersLabel"),

        // ==================================================================
        // CnCNet Lobby
        // ==================================================================
        "Online:".L10N("Client:UI:LobbyOnlineLabel"),
        "Channel:".L10N("Client:UI:LobbyChannelLabel"),
        "Color:".L10N("Client:UI:LobbyColorLabel"),
        "(Admin)".L10N("Client:UI:LobbyAdminSuffix"),
        "Notice".L10N("Client:UI:LobbyNoticeTitle"),
        "Game Invitation".L10N("Client:UI:LobbyGameInvitationTitle"),
        " has invited you to ".L10N("Client:UI:LobbyGameInvitationText"),
        "Loaded game".L10N("Client:UI:LobbyLoadedGame"),
        "Refresh".L10N("Client:UI:LobbyRefresh"),
        "Create Game".L10N("Client:UI:ButtonCreateGame"),
        "Join Game".L10N("Client:UI:ButtonJoinGame"),

        // ==================================================================
        // CnCNet Lobby - Separators and Emoji (non-translatable visual elements)
        // ==================================================================
        // "  |  " separators, lock/cross/no-entry emoji, star/no-entry emoji
        // are visual-only and should not be translated.
        // "  |  ".L10N("Client:UI:LobbySeparator"),

        // ==================================================================
        // CnCNet Lobby - Watermarks
        // ==================================================================
        "Filter by name, map, game mode, player...".L10N("Client:UI:WatermarkFilterGames"),
        "Type here to chat...".L10N("Client:UI:WatermarkChat"),

        // ==================================================================
        // LAN Lobby
        // ==================================================================
        "Create Game".L10N("Client:UI:ButtonCreateGame"),
        "Join Game".L10N("Client:UI:ButtonJoinGame"),
        "Main Menu".L10N("Client:UI:ButtonMainMenu"),

        // ==================================================================
        // CnCNet Login Window
        // ==================================================================
        "CnCNet Login".L10N("Client:UI:LoginTitle"),
        "Username:".L10N("Client:UI:LoginUsername"),
        "Remember my username".L10N("Client:UI:LoginRememberMe"),

        // ==================================================================
        // Game Creation Window
        // ==================================================================
        "Create Game".L10N("Client:UI:CreateGameTitle"),
        "Game Name:".L10N("Client:UI:CreateGameName"),
        "Password (optional):".L10N("Client:UI:CreateGamePassword"),
        "Max Players:".L10N("Client:UI:CreateGameMaxPlayers"),
        "Tunnel Server:".L10N("Client:UI:CreateGameTunnelServer"),
        "Skill Level:".L10N("Client:UI:CreateGameSkillLevel"),
        "Private Game".L10N("Client:UI:CreateGamePrivateGame"),
        "Advanced Options".L10N("Client:UI:CreateGameAdvancedOptions"),

        // ==================================================================
        // Password Request Window
        // ==================================================================
        "Game:".L10N("Client:UI:PasswordGameLabel"),
        "Host:".L10N("Client:UI:PasswordHostLabel"),
        "Password:".L10N("Client:UI:PasswordLabel"),

        // ==================================================================
        // Tunnel Selection Window
        // ==================================================================
        "Confirm".L10N("Client:UI:ButtonConfirm"),

        // ==================================================================
        // Private Messaging Window
        // ==================================================================
        "PRIVATE MESSAGING".L10N("Client:UI:PmHeader"),
        "MESSAGES:".L10N("Client:UI:PmMessages"),
        "Messages".L10N("Client:UI:PmTabMessages"),
        "Friend List".L10N("Client:UI:PmTabFriendList"),
        "All Players".L10N("Client:UI:PmTabAllPlayers"),
        "Recent Players".L10N("Client:UI:PmTabRecentPlayers"),
        "New Private Message".L10N("Client:UI:PmNewMessageTitle"),
        "Type your message...".L10N("Client:UI:WatermarkTypeMessage"),

        // ==================================================================
        // Map Sharing Confirmation Panel
        // ==================================================================
        "Map Download Required".L10N("Client:UI:MapSharingTitle"),
        "Map:".L10N("Client:UI:MapSharingMap"),
        "Host:".L10N("Client:UI:MapSharingHost"),

        // ==================================================================
        // Load Or Save Game Option Preset Window
        // ==================================================================
        "Preset Name:".L10N("Client:UI:PresetName"),

        // ==================================================================
        // Skirmish Lobby
        // ==================================================================
        "PLAYER".L10N("Client:UI:PlayerOptionPlayer"),
        "SIDE".L10N("Client:UI:PlayerOptionSide"),
        "COLOR".L10N("Client:UI:PlayerOptionColor"),
        "TEAM".L10N("Client:UI:PlayerOptionTeam"),
        "START".L10N("Client:UI:PlayerOptionStart"),
        "Random".L10N("Client:UI:ButtonRandom"),
        "Error".L10N("Client:UI:ErrorTitle"),

        // ==================================================================
        // Game Lobby (LANGameLobby / CnCNetGameLobby)
        // ==================================================================
        "GAME MODE:".L10N("Client:UI:GameLobbyGameMode"),
        "Filter by name, map, game mode...".L10N("Client:UI:WatermarkFilterMaps"),
        "Type a message...".L10N("Client:UI:WatermarkTypeMessageLobby"),

        // ==================================================================
        // LAN Game Creation Window
        // ==================================================================
        "SELECT SESSION TYPE".L10N("Client:UI:LanSessionTypeTitle"),
        "New Game".L10N("Client:UI:ButtonNewGame"),
        "Load Game".L10N("Client:UI:ButtonLoadGame"),

        // ==================================================================
        // LAN Game Loading Lobby
        // ==================================================================
        "Wait for all players to join and get ready, then click Load Game to load the saved multiplayer game.".L10N("Client:UI:LanGameLoadingHint"),
        "MAP:".L10N("Client:UI:LanGameLoadingMap"),
        "GAME MODE:".L10N("Client:UI:LanGameLoadingGameMode"),
        "SAVED GAME:".L10N("Client:UI:LanGameLoadingSavedGame"),
    };
    // ReSharper restore InconsistentNaming
}
