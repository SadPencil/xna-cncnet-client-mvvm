using AvClientMvvmContract.Domain;
using AvClientMvvmContract.Campaign;

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using AvClientViewModel.Domain;

using Rampastring.Tools;

using Serilog;

using ClientCore.Settings;

namespace AvClientViewModel.Campaign
{
    public partial class CampaignSelectorViewModel : ObservableObject, ICampaignSelectorViewModel
    {
        private const string SETTINGS_PATH = "Client/CampaignSettings.ini";

        private static string[] DifficultyNamesArray = new string[] { "Easy", "Medium", "Hard" };

        private static string[] DifficultyIniPaths = new string[]
        {
            "INI/Map Code/Difficulty Easy.ini",
            "INI/Map Code/Difficulty Medium.ini",
            "INI/Map Code/Difficulty Hard.ini"
        };

        private readonly IDiscordHandlerService discordHandler;
        private readonly ICampaignGameProcessService gameProcessService;
        private readonly IFileIntegrityService fileIntegrityService;

        private List<Mission> selectedMissions = [];

        // Mission preview paths (View uses these to render preview panel)
        public string MissionPreviewFolder => SafePath.CombineDirectoryPath(ProgramConstants.GetBaseResourcePath(), "Mission Previews");
        public string DefaultMissionPreviewPath => SafePath.CombineFilePath(MissionPreviewFolder, "Default.png");
        public bool IsMissionPreviewEnabled => File.Exists(DefaultMissionPreviewPath);

        // CheckBoxes and DropDowns: created by View from INI, registered here for business logic
        public List<CampaignCheckBoxOption> CheckBoxOptions { get; } = new();
        List<ICampaignCheckBoxOption> ICampaignSelectorViewModel.CheckBoxOptions => CheckBoxOptions.Cast<ICampaignCheckBoxOption>().ToList();
        public List<CampaignDropDownOption> DropDownOptions { get; } = new();
        List<ICampaignDropDownOption> ICampaignSelectorViewModel.DropDownOptions => DropDownOptions.Cast<ICampaignDropDownOption>().ToList();


        // User settings: created by View from INI, registered here for save/load/reset
        public List<IUserSetting> UserSettings { get; } = new();

        private IniFile? gameOptionsIni;

        private string[] filesToCheck = new string[]
        {
            "INI/AI.ini",
            "INI/AIE.ini",
            "INI/Art.ini",
            "INI/ArtE.ini",
            "INI/Enhance.ini",
            "INI/Rules.ini",
            "INI/Map Code/Difficulty Hard.ini",
            "INI/Map Code/Difficulty Medium.ini",
            "INI/Map Code/Difficulty Easy.ini"
        };

        private Mission? missionToLaunch;

        private List<Mission> _allMissions = [];
        public IReadOnlyCollection<IMission> AllMissions { get => _allMissions; }

        private Dictionary<int, Mission> _uniqueIDToMissions = new();
        public IReadOnlyDictionary<int, IMission> UniqueIDToMissions => (IReadOnlyDictionary<int, IMission>)_uniqueIDToMissions;

        private readonly Action? onReturnRequested;

        public CampaignSelectorViewModel(
            IDiscordHandlerService discordHandler,
            ICampaignGameProcessService gameProcessService,
            IFileIntegrityService fileIntegrityService,
            Action? onReturnRequested = null)
        {
            this.discordHandler = discordHandler;
            this.gameProcessService = gameProcessService;
            this.fileIntegrityService = fileIntegrityService;
            this.onReturnRequested = onReturnRequested;

            CheaterWindow = new CheaterWindowViewModel(OnCheaterConfirmed, OnCheaterCancelled);

            gameProcessService.GameProcessExited += OnGameProcessExited;

            // Initialize() equivalent - all non-UI initialization
            gameOptionsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(),
                ClientConfiguration.GAME_OPTIONS));

            SelectedDifficultyIndex = UserINISettings.Instance.Difficulty;

            ReadMissionList();

            LoadSettings();
        }

        #region Observable Properties

        [ObservableProperty]
        private IReadOnlyList<ICampaignListItem> campaignListItems = [];

        [ObservableProperty]
        private int selectedCampaignIndex = -1;

        [ObservableProperty]
        private string missionDescriptionText = string.Empty;

        [ObservableProperty]
        private string? missionPreviewImagePath;

        [ObservableProperty]
        private bool isControlsEnabled = true;

        [ObservableProperty]
        private bool isVisible;

        [ObservableProperty]
        private IReadOnlyList<string> difficultyNames = DifficultyNamesArray;

        [ObservableProperty]
        private int selectedDifficultyIndex = 1;

        [ObservableProperty]
        private bool canLaunchCampaign;

        [ObservableProperty]
        private bool isCheaterWindowVisible;

        public bool IsReturnButtonVisible => ClientConfiguration.Instance.CampaignTagSelectorEnabled;

        public ICheaterWindowViewModel CheaterWindow { get; }

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
                // Confront the user by showing the cheater screen
                missionToLaunch = mission;
                IsCheaterWindowVisible = true;
                return;
            }

            LaunchMission(mission);
        }

        [RelayCommand]
        private void Return()
        {
            onReturnRequested?.Invoke();
        }

        [RelayCommand]
        private void Cancel()
        {
            SaveSettings();
            IsVisible = false;
        }

        [RelayCommand]
        private void Refresh()
        {
            ReadMissionList();
        }

        #endregion

        #region Selection Changed (replaces LbCampaignList_SelectedIndexChanged)

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

            // UpdateMissionPreview equivalent
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

        private void AddMission(Mission mission)
        {
            // no matter whether the key is duplicated, the mission is always added to AllMissions
            _allMissions.Add(mission);

            // but only the first mission is recorded in UniqueIDToMissions
            if (_uniqueIDToMissions.ContainsKey(mission.CustomMissionID))
            {
                Log.Information($"CampaignSelector: duplicated mission. CodeName: {mission.CodeName}. ID: {mission.CustomMissionID}. Description: {mission.UntranslatedGUIName}.");
                if (!string.IsNullOrEmpty(mission.Scenario))
                    mission.Enabled = false;
            }
            else
            {
                _uniqueIDToMissions.Add(mission.CustomMissionID, mission);
            }
        }

        private void ReadMissionList()
        {
            ParseBattleIni("INI/Battle.ini");

            if (AllMissions.Count == 0)
                ParseBattleIni("INI/" + ClientConfiguration.Instance.BattleFSFileName);

            LoadCustomMissions();

            LoadMissionsWithFilter(null, disableCustomMissions: true, disableOfficialMissions: false);
        }

        private void LoadCustomMissions()
        {
            string customMissionsDirectory = SafePath.CombineDirectoryPath(ProgramConstants.GamePath, ClientConfiguration.Instance.CustomMissionPath);
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

        /// <summary>
        /// Parses a Battle(E).ini file. Returns true if succesful (file found), otherwise false.
        /// </summary>
        /// <param name="path">The path of the file, relative to the game directory.</param>
        /// <returns>True if succesful, otherwise false.</returns>
        private bool ParseBattleIni(string path)
        {
            Log.Information("Attempting to parse " + path + " to populate mission list.");

            FileInfo battleIniFileInfo = SafePath.GetFile(ProgramConstants.GamePath, path);
            if (!battleIniFileInfo.Exists)
            {
                Log.Warning("File " + path + " not found. Ignoring.");
                return false;
            }

            if (selectedMissions.Count > 0)
            {
                throw new InvalidOperationException("Loading multiple Battle*.ini files is not supported anymore.");
            }

            var battleIni = new IniFile(battleIniFileInfo.FullName);

            List<string>? battleKeys = battleIni.GetSectionKeys("Battles");

            if (battleKeys == null)
                return false; // File exists but [Battles] doesn't

            for (int i = 0; i < battleKeys.Count; i++)
            {
                string battleEntry = battleKeys[i];
                string battleSection = battleIni.GetStringValue("Battles", battleEntry, "NOT FOUND");

                if (!battleIni.SectionExists(battleSection))
                    continue;

                var mission = new Mission(battleIni.GetSection(battleSection), missionCodeName: battleEntry);
                AddMission(mission);
            }

            Log.Information("Finished parsing " + path + ".");
            return true;
        }

        /// <summary>
        /// Load or re-load missons with selected tags.
        /// </summary>
        /// <param name="selectedTags">Missions with at lease one of which tags to be shown. As an exception, null means show all missions.</param>
        public void LoadMissionsWithFilter(ISet<string>? selectedTags, bool disableCustomMissions = true, bool disableOfficialMissions = false)
        {
            selectedMissions.Clear();
            SelectedCampaignIndex = -1;

            // Select missions with the filter
            IEnumerable<Mission> missions = AllMissions.Cast<Mission>();
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
            else
            {
                // do nothing
            }

            if (selectedTags != null)
                missions = missions.Where(mission => mission.Tags.Intersect(selectedTags).Any()).ToList();
            selectedMissions = missions.ToList();

            // Build CampaignListItem list (replaces XNAListBoxItem creation in original)
            var items = new List<CampaignListItem>(selectedMissions.Count);
            foreach (Mission mission in selectedMissions)
            {
                CampaignListItemColor textColorKind;
                bool isHeader = false;
                bool isSelectable = true;

                if (!mission.Enabled)
                {
                    textColorKind = CampaignListItemColor.Disabled;
                }
                else if (string.IsNullOrEmpty(mission.Scenario))
                {
                    textColorKind = CampaignListItemColor.Header;
                    isHeader = true;
                    isSelectable = false;
                }
                else
                {
                    textColorKind = CampaignListItemColor.Default;
                }

                string? iconPath = null;
                if (!string.IsNullOrEmpty(mission.IconPath))
                    iconPath = mission.IconPath + "icon.png";

                items.Add(new CampaignListItem
                {
                    Text = mission.GUIName,
                    IsEnabled = mission.Enabled,
                    IsHeader = isHeader,
                    IsSelectable = isSelectable,
                    IconPath = iconPath,
                    TextColorKind = textColorKind
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
                Log.Warning($"CampaignSelector: mission scenario contains invalid path characters. Mission code name: {mission.CodeName}. Scenario: {mission.Scenario}. This mission will be launched without applying {nameof(ClientConfiguration.Instance.CopyMissionsToSpawnmapINI)}.");
                copyMapsToSpawnmapINI = false;
            }

            Log.Information("About to write spawn.ini.");
            IniFile spawnIni = new(spawnerSettingsFile.FullName)
            {
                Comment = "Generated by CnCNet Client"
            };
            IniSection spawnIniSettings = new("Settings");

            if (copyMapsToSpawnmapINI)
                spawnIniSettings.AddKey("Scenario", "spawnmap.ini");
            else
                spawnIniSettings.AddKey("Scenario", scenario);

            // No one wants to play missions on Fastest, so we'll change it to Faster
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
                    // TODO figure out the RA one
            }

            spawnIniSettings.AddKey("CustomLoadScreen", LoadingScreenController.GetLoadScreenName(mission.Side.ToString()));

            spawnIniSettings.AddKey("IsSinglePlayer", "Yes");
            spawnIniSettings.AddKey("SidebarHack", ClientConfiguration.Instance.SidebarHack.ToString(CultureInfo.InvariantCulture));
            spawnIniSettings.AddKey("Side", mission.Side.ToString(CultureInfo.InvariantCulture));
            spawnIniSettings.AddKey("BuildOffAlly", mission.BuildOffAlly.ToString(CultureInfo.InvariantCulture));

            UserINISettings.Instance.Difficulty.Value = SelectedDifficultyIndex;

            spawnIniSettings.AddKey("DifficultyModeHuman", mission.PlayerAlwaysOnNormalDifficulty ? "1" : SelectedDifficultyIndex.ToString(CultureInfo.InvariantCulture));
            spawnIniSettings.AddKey("DifficultyModeComputer", GetComputerDifficulty().ToString(CultureInfo.InvariantCulture));

            if (mission.IsCustomMission)
            {
                spawnIniSettings.AddKey("CustomMissionID", mission.CustomMissionID.ToString(CultureInfo.InvariantCulture));
            }

            spawnIni.AddSection(spawnIniSettings);
            WriteMissionSectionToSpawnIni(spawnIni, mission);

            foreach (CampaignCheckBoxOption chkBox in CheckBoxOptions)
                chkBox.ApplySpawnIniCode(spawnIni);
            foreach (CampaignDropDownOption dd in DropDownOptions)
                dd.ApplySpawnIniCode(spawnIni);

            // Apply forced options from GameOptions.ini

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

                foreach (CampaignCheckBoxOption chkBox in CheckBoxOptions)
                    chkBox.ApplyMapCode(mapIni, gameMode: null);
                foreach (CampaignDropDownOption dd in DropDownOptions)
                    dd.ApplyMapCode(mapIni, gameMode: null);

                mapIni.WriteIniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawnmap.ini"));
            }

            UserINISettings.Instance.Difficulty.Value = SelectedDifficultyIndex;
            UserINISettings.Instance.SaveSettings();

            if (ClientConfiguration.Instance.ReturnToMainMenuOnMissionLaunch)
                IsVisible = false;
            else
                IsControlsEnabled = false;

            discordHandler.SetCampaignPresence(mission.UntranslatedGUIName, difficultyName, mission.IconPath, true);
            gameProcessService.StartGameProcess();
        }

        public static void WriteMissionSectionToSpawnIni(IniFile spawnIni, Mission mission)
        {
            bool hasGameMissionData = false;

            bool scenarioPathFound = mission.TryGetScenarioFilePath(out string scenarioPath);
            if (!scenarioPathFound)
            {
                Log.Warning($"CampaignSelector: mission scenario contains invalid path characters. Mission code name: {mission.CodeName}. Scenario: {mission.Scenario}. This mission will be launched without mission section data.");
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
                // copy an IniSection
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

                // append the new IniSection
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

        #region Cheater Window

        private void OnCheaterConfirmed()
        {
            IsCheaterWindowVisible = false;
            LaunchMission(missionToLaunch!);
        }

        private void OnCheaterCancelled()
        {
            IsCheaterWindowVisible = false;
            missionToLaunch = null;
        }

        #endregion

        #region Game Process

        private void OnGameProcessExited()
        {
            CustomMissionHelper.DeleteSupplementalMissionFiles();

            // Log.Information("GameProcessExited: Updating Discord Presence.");
            discordHandler.SetMainMenuPresence();

            if (!ClientConfiguration.Instance.ReturnToMainMenuOnMissionLaunch)
                IsControlsEnabled = true;

            // Handle ResetToDefaultOnGameExit
            {
                // Reset campaign checkboxes
                foreach (CampaignCheckBoxOption cb in CheckBoxOptions)
                {
                    if (cb.ResetToDefaultOnGameExit)
                        cb.ResetToDefault();
                }

                // Reset user settings
                foreach (IUserSetting setting in UserSettings)
                {
                    if (!setting.ResetToDefaultOnGameExit)
                        continue;

                    setting.ResetToDefault();
                }

                SaveSettings();
            }
        }

        #endregion

        #region Settings

        /// <summary>
        /// Saves settings to an INI file on the file system.
        /// </summary>
        private void SaveSettings()
        {
            SaveUserSettings();
            SaveCampaignSettings();
        }

        private void SaveUserSettings()
        {
            UserSettings.ForEach(c => c.Save());
            UserINISettings.Instance.SaveSettings();
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

                foreach (ICampaignDropDownOption dd in DropDownOptions)
                    settingsIni.SetStringValue("GameOptions", dd.Name, dd.SelectedIndex.ToString());

                foreach (ICampaignCheckBoxOption cb in CheckBoxOptions)
                    settingsIni.SetStringValue("GameOptions", cb.Name, cb.Checked.ToString());

                settingsIni.WriteIniFile();
            }
            catch (Exception ex)
            {
                Log.Warning($"Saving campaign settings failed! Reason: {ex}");
            }
        }

        /// <summary>
        /// Loads settings from an INI file on the file system.
        /// </summary>
        private void LoadSettings()
        {
            LoadUserSettings();
            LoadCampaignSettings();
        }

        private void LoadUserSettings()
        {
            UserSettings.ForEach(c => c.Load());
        }

        private void LoadCampaignSettings()
        {
            if (!ClientConfiguration.Instance.SaveCampaignGameOptions)
                return;

            var settingsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, SETTINGS_PATH));

            foreach (ICampaignDropDownOption dd in DropDownOptions)
            {
                dd.SelectedIndex = settingsIni.GetIntValue("GameOptions", dd.Name, dd.SelectedIndex);

                if (dd.SelectedIndex > -1 && dd.SelectedIndex < dd.ItemCount)
                    dd.SelectedIndex = dd.SelectedIndex;
            }

            foreach (ICampaignCheckBoxOption cb in CheckBoxOptions)
                cb.Checked = settingsIni.GetBooleanValue("GameOptions", cb.Name, cb.Checked);
        }

        #endregion
    }
}


