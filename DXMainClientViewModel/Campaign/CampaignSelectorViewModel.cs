#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain;

using Rampastring.Tools;

namespace DXMainClientViewModel.Campaign;

public partial class CampaignSelectorViewModel : ObservableObject, ICampaignSelectorViewModel
{
    private const string SETTINGS_PATH = "Client/CampaignSettings.ini";

    private static readonly string[] DifficultyNamesArray = ["Easy", "Medium", "Hard"];

    private static readonly string[] DifficultyIniPaths =
    [
        "INI/Map Code/Difficulty Easy.ini",
        "INI/Map Code/Difficulty Medium.ini",
        "INI/Map Code/Difficulty Hard.ini"
    ];

    private readonly string[] filesToCheck =
    [
        "INI/AI.ini",
        "INI/AIE.ini",
        "INI/Art.ini",
        "INI/ArtE.ini",
        "INI/Enhance.ini",
        "INI/Rules.ini",
        "INI/Map Code/Difficulty Hard.ini",
        "INI/Map Code/Difficulty Medium.ini",
        "INI/Map Code/Difficulty Easy.ini"
    ];

    private readonly IDiscordHandlerService discordHandler;
    private readonly ICampaignGameProcessService gameProcessService;
    private readonly IFileIntegrityService fileIntegrityService;

    private List<Mission> allMissions = [];
    private Dictionary<int, Mission> uniqueIDToMissions = new();
    private List<Mission> selectedMissions = [];
    private Mission? missionToLaunch;
    private IniFile? gameOptionsIni;

    public CampaignSelectorViewModel(
        IDiscordHandlerService discordHandler,
        ICampaignGameProcessService gameProcessService,
        IFileIntegrityService fileIntegrityService)
    {
        this.discordHandler = discordHandler;
        this.gameProcessService = gameProcessService;
        this.fileIntegrityService = fileIntegrityService;

        CheaterWindow = new CheaterWindowViewModel(OnCheaterConfirmed, OnCheaterCancelled);

        gameProcessService.GameProcessExited += OnGameProcessExited;
    }

    #region Observable Properties

    [ObservableProperty]
    private IReadOnlyList<CampaignListItem> campaignListItems = [];

    [ObservableProperty]
    private int selectedCampaignIndex = -1;

    [ObservableProperty]
    private string missionDescriptionText = string.Empty;

    [ObservableProperty]
    private string? missionPreviewImagePath;

    [ObservableProperty]
    private bool isMissionPreviewPanelVisible;

    [ObservableProperty]
    private bool isReturnButtonVisible;

    [ObservableProperty]
    private bool isControlsEnabled = true;

    [ObservableProperty]
    private IReadOnlyList<string> difficultyNames = DifficultyNamesArray;

    [ObservableProperty]
    private int selectedDifficultyIndex = 1;

    [ObservableProperty]
    private bool canLaunchCampaign;

    [ObservableProperty]
    private bool isCheaterWindowVisible;

    public ICheaterWindowViewModel CheaterWindow { get; }

    public IReadOnlyCollection<Mission> AllMissions => allMissions;
    public IReadOnlyDictionary<int, Mission> UniqueIDToMissions => uniqueIDToMissions;

    #endregion

    #region Commands

    [RelayCommand]
    private void LaunchCampaign()
    {
        SaveSettings();

        if (SelectedCampaignIndex < 0 || SelectedCampaignIndex >= selectedMissions.Count)
            return;

        Mission mission = selectedMissions[SelectedCampaignIndex];

        if (!ClientConfiguration.Instance.ModMode &&
            (!fileIntegrityService.IsFileNonexistantOrOriginal(mission.Scenario) || AreFilesModified()))
        {
            missionToLaunch = mission;
            SetCheaterWindowText();
            IsCheaterWindowVisible = true;
            return;
        }

        LaunchMission(mission);
    }

    [RelayCommand]
    private void Return()
    {
        // The View's parent (CampaignTagSelector) handles the actual navigation.
        // This command signals the intent to return.
    }

    [RelayCommand]
    private void Cancel()
    {
        SaveSettings();
    }

    [RelayCommand]
    private void Refresh()
    {
        ReadMissionList();
    }

    #endregion

    #region Initialization

    public void Initialize()
    {
        gameOptionsIni = new IniFile(SafePath.CombineFilePath(
            ProgramConstants.GetBaseResourcePath(),
            ClientConfiguration.GAME_OPTIONS));

        ReadMissionList();

        LoadSettings();
    }

    #endregion

    #region Selection Changed

    partial void OnSelectedCampaignIndexChanged(int value)
    {
        if (value < 0 || value >= selectedMissions.Count)
        {
            MissionDescriptionText = string.Empty;
            MissionPreviewImagePath = null;
            CanLaunchCampaign = false;
            return;
        }

        Mission mission = selectedMissions[value];
        MissionPreviewImagePath = string.IsNullOrEmpty(mission.PreviewImage) ? null : mission.PreviewImage;

        if (string.IsNullOrEmpty(mission.Scenario))
        {
            MissionDescriptionText = string.Empty;
            CanLaunchCampaign = false;
            return;
        }

        MissionDescriptionText = mission.GUIDescription;

        if (!mission.Enabled)
        {
            CanLaunchCampaign = false;
            return;
        }

        CanLaunchCampaign = true;
    }

    #endregion

    #region Mission List Management

    private void ReadMissionList()
    {
        ParseBattleIni("INI/Battle.ini");

        if (allMissions.Count == 0)
            ParseBattleIni("INI/" + ClientConfiguration.Instance.BattleFSFileName);

        LoadCustomMissions();

        LoadMissionsWithFilter(null, disableCustomMissions: true, disableOfficialMissions: false);
    }

    private bool ParseBattleIni(string path)
    {
        Logger.Log("Attempting to parse " + path + " to populate mission list.");

        FileInfo battleIniFileInfo = SafePath.GetFile(ProgramConstants.GamePath, path);
        if (!battleIniFileInfo.Exists)
        {
            Logger.Log("File " + path + " not found. Ignoring.");
            return false;
        }

        if (selectedMissions.Count > 0)
        {
            throw new InvalidOperationException("Loading multiple Battle*.ini files is not supported anymore.");
        }

        var battleIni = new IniFile(battleIniFileInfo.FullName);

        List<string>? battleKeys = battleIni.GetSectionKeys("Battles");

        if (battleKeys == null)
            return false;

        for (int i = 0; i < battleKeys.Count; i++)
        {
            string battleEntry = battleKeys[i];
            string battleSection = battleIni.GetStringValue("Battles", battleEntry, "NOT FOUND");

            if (!battleIni.SectionExists(battleSection))
                continue;

            var mission = new Mission(battleIni.GetSection(battleSection), missionCodeName: battleEntry);
            AddMission(mission);
        }

        Logger.Log("Finished parsing " + path + ".");
        return true;
    }

    private void LoadCustomMissions()
    {
        string customMissionsDirectory = SafePath.CombineDirectoryPath(
            ProgramConstants.GamePath, ClientConfiguration.Instance.CustomMissionPath);
        if (!Directory.Exists(customMissionsDirectory))
            return;

        string[] mapFiles = Directory.GetFiles(customMissionsDirectory, "*.map");
        if (mapFiles.Length == 0)
            return;

        foreach (string mapFilePath in mapFiles)
        {
            var mapFile = new IniFile(mapFilePath);

            IniSection? clientMissionDataSection = mapFile.GetSection("ClientMissionConfig");

            if (clientMissionDataSection is null)
                continue;

            IniSection? gameMissionDataSection = mapFile.GetSection("GameMissionConfig");

            string filename = new FileInfo(mapFilePath).Name;
            string scenario = SafePath.CombineFilePath(ClientConfiguration.Instance.CustomMissionPath, filename);
            Mission mission = Mission.NewCustomMission(clientMissionDataSection, missionCodeName: filename, scenario, gameMissionDataSection);
            AddMission(mission);
        }
    }

    private void AddMission(Mission mission)
    {
        allMissions.Add(mission);

        if (uniqueIDToMissions.ContainsKey(mission.CustomMissionID))
        {
            Logger.Log($"CampaignSelector: duplicated mission. CodeName: {mission.CodeName}. ID: {mission.CustomMissionID}. Description: {mission.UntranslatedGUIName}.");
            if (!string.IsNullOrEmpty(mission.Scenario))
                mission.Enabled = false;
        }
        else
        {
            uniqueIDToMissions.Add(mission.CustomMissionID, mission);
        }
    }

    public void LoadMissionsWithFilter(ISet<string>? selectedTags, bool disableCustomMissions = true, bool disableOfficialMissions = false)
    {
        selectedMissions.Clear();
        SelectedCampaignIndex = -1;

        IEnumerable<Mission> missions = allMissions;
        if (disableCustomMissions && disableOfficialMissions)
        {
            // do nothing
        }
        else if (disableCustomMissions)
        {
            missions = missions.Where(mission => !mission.IsCustomMission);
        }
        else if (disableOfficialMissions)
        {
            missions = missions.Where(mission => mission.IsCustomMission);
        }

        if (selectedTags != null)
            missions = missions.Where(mission => mission.Tags.Intersect(selectedTags).Any()).ToList();
        selectedMissions = missions.ToList();

        var items = new List<CampaignListItem>(selectedMissions.Count);
        foreach (Mission mission in selectedMissions)
        {
            items.Add(new CampaignListItem
            {
                Text = mission.GUIName,
                IsEnabled = mission.Enabled,
                IsHeader = !mission.Enabled || (string.IsNullOrEmpty(mission.Scenario) && mission.Enabled),
                IsSelectable = mission.Enabled && !string.IsNullOrEmpty(mission.Scenario),
                IconPath = string.IsNullOrEmpty(mission.IconPath) ? null : mission.IconPath + "icon.png"
            });
        }

        CampaignListItems = items;
    }

    #endregion

    #region Mission Launch

    private void LaunchMission(Mission mission)
    {
        CustomMissionHelper.CopySupplementalMissionFiles(mission);

        FileInfo spawnerSettingsFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);

        spawnerSettingsFile.Delete();

        bool copyMapsToSpawnmapINI = ClientConfiguration.Instance.CopyMissionsToSpawnmapINI;

        string scenario = mission.Scenario;
        bool scenarioPathFound = mission.TryGetScenarioFilePath(out string scenarioPath);

        if (!scenarioPathFound)
        {
            Logger.Log($"CampaignSelector: mission scenario contains invalid path characters. Mission code name: {mission.CodeName}. Scenario: {mission.Scenario}. This mission will be launched without applying {nameof(ClientConfiguration.Instance.CopyMissionsToSpawnmapINI)}.");
            copyMapsToSpawnmapINI = false;
        }

        Logger.Log("About to write spawn.ini.");
        IniFile spawnIni = new(spawnerSettingsFile.FullName)
        {
            Comment = "Generated by CnCNet Client"
        };
        IniSection spawnIniSettings = new("Settings");

        if (copyMapsToSpawnmapINI)
            spawnIniSettings.AddKey("Scenario", "spawnmap.ini");
        else
            spawnIniSettings.AddKey("Scenario", scenario);

        if (UserINISettings.Instance.GameSpeed == 0)
            UserINISettings.Instance.GameSpeed.Value = 1;

        spawnIniSettings.AddKey("CampaignID", mission.CampaignID.ToString(CultureInfo.InvariantCulture));
        spawnIniSettings.AddKey("GameSpeed", UserINISettings.Instance.GameSpeed.ToString());

        switch (ClientConfiguration.Instance.ClientGameType)
        {
            case ClientType.YR or ClientType.Ares:
                spawnIniSettings.AddKey("Ra2Mode", (!mission.RequiredAddon).ToString(CultureInfo.InvariantCulture));
                break;
            case ClientType.TS:
                spawnIniSettings.AddKey("Firestorm", mission.RequiredAddon.ToString(CultureInfo.InvariantCulture));
                break;
        }

        spawnIniSettings.AddKey("CustomLoadScreen", LoadingScreenController.GetLoadScreenName(mission.Side.ToString()));

        spawnIniSettings.AddKey("IsSinglePlayer", "Yes");
        spawnIniSettings.AddKey("SidebarHack", ClientConfiguration.Instance.SidebarHack.ToString(CultureInfo.InvariantCulture));
        spawnIniSettings.AddKey("Side", mission.Side.ToString(CultureInfo.InvariantCulture));
        spawnIniSettings.AddKey("BuildOffAlly", mission.BuildOffAlly.ToString(CultureInfo.InvariantCulture));

        spawnIniSettings.AddKey("DifficultyModeHuman", mission.PlayerAlwaysOnNormalDifficulty ? "1" : SelectedDifficultyIndex.ToString(CultureInfo.InvariantCulture));
        spawnIniSettings.AddKey("DifficultyModeComputer", GetComputerDifficulty().ToString(CultureInfo.InvariantCulture));

        if (mission.IsCustomMission)
        {
            spawnIniSettings.AddKey("CustomMissionID", mission.CustomMissionID.ToString(CultureInfo.InvariantCulture));
        }

        spawnIni.AddSection(spawnIniSettings);
        WriteMissionSectionToSpawnIni(spawnIni, mission);

        List<string>? forcedKeys = gameOptionsIni?.GetSectionKeys("CampaignForcedSpawnIniOptions");

        if (forcedKeys != null)
        {
            foreach (string key in forcedKeys)
            {
                spawnIni.SetStringValue("Settings", key,
                    gameOptionsIni!.GetStringValue("CampaignForcedSpawnIniOptions", key, String.Empty));
            }
        }

        spawnIni.WriteIniFile();

        var difficultyIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, DifficultyIniPaths[SelectedDifficultyIndex]));
        string difficultyName = DifficultyNamesArray[SelectedDifficultyIndex];

        if (copyMapsToSpawnmapINI)
        {
            var mapIni = new IniFile(scenarioPath);
            IniFile.ConsolidateIniFiles(mapIni, difficultyIni);
            mapIni.WriteIniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawnmap.ini"));
        }

        UserINISettings.Instance.Difficulty.Value = SelectedDifficultyIndex;
        UserINISettings.Instance.SaveSettings();

        IsControlsEnabled = false;

        discordHandler.SetCampaignPresence(mission.UntranslatedGUIName, difficultyName);
        gameProcessService.StartGameProcess();
    }

    public static void WriteMissionSectionToSpawnIni(IniFile spawnIni, Mission mission)
    {
        bool hasGameMissionData = false;

        bool scenarioPathFound = mission.TryGetScenarioFilePath(out string scenarioPath);
        if (!scenarioPathFound)
        {
            Logger.Log($"CampaignSelector: mission scenario contains invalid path characters. Mission code name: {mission.CodeName}. Scenario: {mission.Scenario}. This mission will be launched without mission section data.");
            return;
        }

        if (!mission.IsCustomMission && File.Exists(scenarioPath))
        {
            var mapIni = new IniFile(scenarioPath);
            mission.GameMissionConfigSection = mapIni.GetSection("GameMissionConfig");

            if (mission.GameMissionConfigSection is not null)
                hasGameMissionData = true;
        }

        if (mission.IsCustomMission && mission.GameMissionConfigSection is not null || hasGameMissionData)
        {
            IniSection spawnIniMissionIniSection = new(mission.Scenario);
            string loadingScreenName = string.Empty;
            string loadingScreenPalName = string.Empty;
            foreach (var kvp in mission.GameMissionConfigSection!.Keys)
            {
                if (string.IsNullOrEmpty(kvp.Value))
                {
                    if (kvp.Key.Equals("LS640BkgdName", StringComparison.InvariantCulture) || kvp.Key.Equals("LS800BkgdName", StringComparison.InvariantCulture))
                        loadingScreenName = kvp.Value;
                    else if (kvp.Key.Equals("LS800BkgdPal", StringComparison.InvariantCulture))
                        loadingScreenPalName = kvp.Value;
                }

                spawnIniMissionIniSection.AddKey(kvp.Key, kvp.Value);
            }

            if (string.IsNullOrEmpty(loadingScreenName))
            {
                string lsFilename = CustomMissionHelper.CustomMissionSupplementDefinition?.FirstOrDefault(x => x.extension.Equals("shp", StringComparison.InvariantCultureIgnoreCase)).filename ?? string.Empty;

                if (!string.IsNullOrEmpty(lsFilename))
                {
                    spawnIniMissionIniSection.AddOrReplaceKey("LS640BkgdName", lsFilename);
                    spawnIniMissionIniSection.AddOrReplaceKey("LS800BkgdName", lsFilename);
                }
            }
            if (string.IsNullOrEmpty(loadingScreenPalName))
            {
                string palFilename = CustomMissionHelper.CustomMissionSupplementDefinition?.FirstOrDefault(x => x.extension.Equals("pal", StringComparison.InvariantCultureIgnoreCase)).filename ?? string.Empty;

                if (!string.IsNullOrEmpty(palFilename))
                    spawnIniMissionIniSection.AddOrReplaceKey("LS800BkgdPal", palFilename);
            }

            spawnIni.AddSection(spawnIniMissionIniSection);
            spawnIni.SetStringValue("Settings", "ReadMissionSection", "Yes");
        }
    }

    private bool AreFilesModified()
    {
        foreach (string filePath in filesToCheck)
        {
            if (!fileIntegrityService.IsFileNonexistantOrOriginal(filePath))
                return true;
        }

        return false;
    }

    private int GetComputerDifficulty() =>
        Math.Abs(SelectedDifficultyIndex - 2);

    #endregion

    #region Game Process

    private void OnGameProcessExited()
    {
        CustomMissionHelper.DeleteSupplementalMissionFiles();
        discordHandler.SetMainMenuPresence();
        IsControlsEnabled = true;
        SaveSettings();
    }

    #endregion

    #region Cheater Window

    private void SetCheaterWindowText()
    {
        if (CheaterWindow is CheaterWindowViewModel vm)
        {
            vm.TitleText = "Modified files detected";
            vm.MessageText = "Game files have been modified. Continue anyway?";
        }
    }

    private void OnCheaterConfirmed()
    {
        IsCheaterWindowVisible = false;
        if (missionToLaunch != null)
            LaunchMission(missionToLaunch);
    }

    private void OnCheaterCancelled()
    {
        IsCheaterWindowVisible = false;
        missionToLaunch = null;
    }

    #endregion

    #region Settings

    private void SaveSettings()
    {
        SaveCampaignSettings();
        UserINISettings.Instance.SaveSettings();
    }

    private void LoadSettings()
    {
        LoadCampaignSettings();
    }

    private void SaveCampaignSettings()
    {
        if (!ClientConfiguration.Instance.SaveCampaignGameOptions)
            return;

        try
        {
            FileInfo settingsFileInfo = SafePath.GetFile(ProgramConstants.GamePath, SETTINGS_PATH);

            settingsFileInfo.Delete();

            var settingsIni = new IniFile(settingsFileInfo.FullName);
            settingsIni.WriteIniFile();
        }
        catch (Exception ex)
        {
            Logger.Log($"Saving campaign settings failed! Reason: {ex}");
        }
    }

    private void LoadCampaignSettings()
    {
        if (!ClientConfiguration.Instance.SaveCampaignGameOptions)
            return;

        var settingsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, SETTINGS_PATH));
        // Settings for checkboxes/dropdowns are handled by the View layer
        // through ICampaignSettingsService if needed in the future.
    }

    #endregion
}
