using DXMainClientMvvmContract.Generic.OptionPanels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;
using ClientCore.I18N;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain;
using DXMainClientViewModel.Services;

using Rampastring.Tools;

namespace DXMainClientViewModel.Generic.OptionPanels;

/// <summary>
/// ViewModel for the display options panel.
/// Contains all business logic from DisplayOptionsPanel.cs except XNA UI rendering.
/// Self-sufficient: populates all data itself via services.
/// </summary>
public partial class DisplayOptionsPanelViewModel : ObservableObject, IDisplayOptionsPanelViewModel
{
    private const int DRAG_DISTANCE_DEFAULT = 4;
    private const int ORIGINAL_RESOLUTION_WIDTH = 640;

    private readonly UserINISettings iniSettings;
    private readonly DirectDrawWrapperManager directDrawWrapperManager;
    private readonly IResolutionProvider resolutionProvider;

    // --- State ---

    private bool gameCompatFixInstalled;
    private bool finalSunCompatFixInstalled;

    // --- Observable state ---

    [ObservableProperty]
    private int _selectedIngameResolutionIndex;

    [ObservableProperty]
    private int _selectedClientResolutionIndex;

    [ObservableProperty]
    private int _selectedDetailLevelIndex;

    [ObservableProperty]
    private int _selectedRendererIndex;

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private int _selectedTranslationIndex;

    [ObservableProperty]
    private bool _isWindowedModeEnabled;

    [ObservableProperty]
    private bool _isBorderlessWindowedModeEnabled;

    [ObservableProperty]
    private bool _isBorderlessWindowedModeAllowed = true;

    [ObservableProperty]
    private bool _isBackBufferStoredInVideoMemory;

    [ObservableProperty]
    private bool _isBorderlessClientEnabled;

    [ObservableProperty]
    private bool _isIntegerScaledClientEnabled;

    [ObservableProperty]
    private bool _isGameCompatFixAvailable;

    [ObservableProperty]
    private bool _isFinalSunCompatFixAvailable;

    [ObservableProperty]
    private bool _isRestartRequired;

    [ObservableProperty]
    private bool _isMessageBoxVisible;

    [ObservableProperty]
    private string _messageBoxTitle = string.Empty;

    [ObservableProperty]
    private string _messageBoxMessage = string.Empty;

    [ObservableProperty]
    private bool _isDirectDrawCompatFixRequired;

    [ObservableProperty]
    private bool _directDrawCompatFixRequiresAdmin;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _ingameResolutionOptions = new();
    public IReadOnlyList<string> IngameResolutionOptions => _ingameResolutionOptions;

    private readonly ObservableCollection<string> _clientResolutionOptions = new();
    public IReadOnlyList<string> ClientResolutionOptions => _clientResolutionOptions;

    private readonly ObservableCollection<string> _detailLevelOptions = new();
    public IReadOnlyList<string> DetailLevelOptions => _detailLevelOptions;

    private readonly ObservableCollection<string> _rendererOptions = new();
    public IReadOnlyList<string> RendererOptions => _rendererOptions;

    private readonly ObservableCollection<string> _themeOptions = new();
    public IReadOnlyList<string> ThemeOptions => _themeOptions;

    private readonly ObservableCollection<string> _translationOptions = new();
    public IReadOnlyList<string> TranslationOptions => _translationOptions;

    // Store theme/translation names for saving
    private readonly List<string> _themeNames = new();
    private readonly List<string> _translationLocaleCodes = new();

    // Store renderer objects for saving
    private readonly List<DirectDrawWrapper> _renderers = new();

    // --- Constructor ---

    public DisplayOptionsPanelViewModel(
        UserINISettings iniSettings,
        DirectDrawWrapperManager directDrawWrapperManager,
        IResolutionProvider resolutionProvider)
    {
        this.iniSettings = iniSettings;
        this.directDrawWrapperManager = directDrawWrapperManager;
        this.resolutionProvider = resolutionProvider;

        PopulateOptions();
        CheckCompatibilityFixes();
    }

    // --- Commands ---

    [RelayCommand]
    private void InstallGameCompatibilityFix()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        if (!gameCompatFixInstalled)
            return;

        try
        {
            Process sdbinst = Process.Start("sdbinst.exe", "-q -n \"TS Compatibility Fix\"");
            sdbinst.WaitForExit();

            Logger.Log("DTA/TI/TS Compatibility Fix succesfully uninstalled.");
            ShowMessageBox(
                "Compatibility Fix Uninstalled".L10N("Client:DTAConfig:TSFixUninstallTitle"),
                "The DTA/TI/TS Compatibility Fix has been succesfully uninstalled.".L10N("Client:DTAConfig:TSFixUninstallText"));

            using var regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
            using var subKey = regKey.CreateSubKey("Tiberian Sun Client");
            subKey.SetValue("TSCompatFixInstalled", "No");

            gameCompatFixInstalled = false;
            IsGameCompatFixAvailable = false;

            if (!finalSunCompatFixInstalled)
                IsFinalSunCompatFixAvailable = false;
        }
        catch (Exception ex)
        {
            Logger.Log("Uninstalling DTA/TI/TS Compatibility Fix failed. Error message: " + ex.ToString());
            ShowMessageBox(
                "Uninstalling Compatibility Fix Failed".L10N("Client:DTAConfig:TSFixUninstallFailTitle"),
                "Uninstalling DTA/TI/TS Compatibility Fix failed. Returned error:".L10N("Client:DTAConfig:TSFixUninstallFailText") + " " + ex.Message);
        }
    }

    [RelayCommand]
    private void InstallMapEditorCompatibilityFix()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        if (!finalSunCompatFixInstalled)
            return;

        try
        {
            Process sdbinst = Process.Start("sdbinst.exe", "-q -n \"Final Sun Compatibility Fix\"");
            sdbinst.WaitForExit();

            using var regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
            using var subKey = regKey.CreateSubKey("Tiberian Sun Client");
            subKey.SetValue("FSCompatFixInstalled", "No");

            Logger.Log("FinalSun Compatibility Fix succesfully uninstalled.");
            ShowMessageBox(
                "Compatibility Fix Uninstalled".L10N("Client:DTAConfig:TSFinalSunFixUninstallTitle"),
                "The FinalSun Compatibility Fix has been succesfully uninstalled.".L10N("Client:DTAConfig:TSFinalSunFixUninstallText"));

            finalSunCompatFixInstalled = false;
            IsFinalSunCompatFixAvailable = false;

            if (!gameCompatFixInstalled)
                IsGameCompatFixAvailable = false;
        }
        catch (Exception ex)
        {
            Logger.Log("Uninstalling FinalSun Compatibility Fix failed. Error message: " + ex.ToString());
            ShowMessageBox(
                "Uninstalling Compatibility Fix Failed".L10N("Client:DTAConfig:TSFinalSunFixUninstallFailedTitle"),
                "Uninstalling FinalSun Compatibility Fix failed. Error message:".L10N("Client:DTAConfig:TSFinalSunFixUninstallFailedText") + " " + ex.Message);
        }
    }

    [RelayCommand]
    private void LoadSettings()
    {
        // Load renderer
        int rendererIndex = _renderers.FindIndex(r => r.InternalName == directDrawWrapperManager.SelectedRenderer.InternalName);
        if (rendererIndex < 0 && directDrawWrapperManager.SelectedRenderer.Hidden)
        {
            _rendererOptions.Add(directDrawWrapperManager.SelectedRenderer.UIName);
            _renderers.Add(directDrawWrapperManager.SelectedRenderer);
            rendererIndex = _rendererOptions.Count - 1;
        }
        SelectedRendererIndex = rendererIndex >= 0 ? rendererIndex : 0;

        SelectedDetailLevelIndex = UserINISettings.Instance.DetailLevel;

        // Load ingame resolution
        string currentRes = UserINISettings.Instance.IngameScreenWidth.Value + "x" + UserINISettings.Instance.IngameScreenHeight.Value;
        int resIndex = _ingameResolutionOptions.ToList().FindIndex(i => i == currentRes);
        SelectedIngameResolutionIndex = resIndex >= 0 ? resIndex : 0;

        // Load client resolution
        string currentClientRes = iniSettings.ClientResolutionX.Value + "x" + iniSettings.ClientResolutionY.Value;
        int clientResIndex = _clientResolutionOptions.ToList().FindIndex(i => i == currentClientRes);
        SelectedClientResolutionIndex = clientResIndex >= 0 ? clientResIndex : 0;

        IsBorderlessClientEnabled = UserINISettings.Instance.BorderlessWindowedClient;
        IsIntegerScaledClientEnabled = iniSettings.IntegerScaledClient.Value;

        // Load theme
        int themeIndex = _themeNames.FindIndex(t => t == UserINISettings.Instance.ClientTheme);
        SelectedThemeIndex = themeIndex >= 0 ? themeIndex : 0;

        // Load translation
        foreach (string localeCode in new string[] { UserINISettings.Instance.Translation, Translation.GetDefaultTranslationLocaleCode(), ProgramConstants.HARDCODED_LOCALE_CODE })
        {
            int transIndex = _translationLocaleCodes.FindIndex(t => t == localeCode);
            if (transIndex >= 0)
            {
                SelectedTranslationIndex = transIndex;
                break;
            }
        }

        // Load windowed mode
        var renderer = directDrawWrapperManager.SelectedRenderer;
        if (renderer.UsesCustomWindowedOption())
        {
            IniFile rendererSettingsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, renderer.ConfigFileName));
            IsWindowedModeEnabled = rendererSettingsIni.GetBooleanValue(renderer.WindowedModeSection, renderer.WindowedModeKey, false);

            if (!string.IsNullOrEmpty(renderer.BorderlessWindowedModeKey))
            {
                bool setting = rendererSettingsIni.GetBooleanValue(renderer.WindowedModeSection, renderer.BorderlessWindowedModeKey, false);
                IsBorderlessWindowedModeEnabled = renderer.IsBorderlessWindowedModeKeyReversed ? !setting : setting;
            }
            else
            {
                IsBorderlessWindowedModeEnabled = UserINISettings.Instance.BorderlessWindowedMode;
            }
        }
        else
        {
            IsWindowedModeEnabled = UserINISettings.Instance.WindowedMode;
            IsBorderlessWindowedModeEnabled = UserINISettings.Instance.BorderlessWindowedMode;
        }

        // Load back buffer
        if (ClientConfiguration.Instance.ClientGameType == ClientType.TS)
            IsBackBufferStoredInVideoMemory = !UserINISettings.Instance.BackBufferInVRAM;
        else
            IsBackBufferStoredInVideoMemory = UserINISettings.Instance.BackBufferInVRAM;

        // TS compat mode
        iniSettings.Win8CompatMode.Value = "No";
    }

    [RelayCommand]
    private void SaveSettings()
    {
        bool restartRequired = false;

        iniSettings.DetailLevel.Value = SelectedDetailLevelIndex;

        // Save ingame resolution
        if (SelectedIngameResolutionIndex >= 0 && SelectedIngameResolutionIndex < _ingameResolutionOptions.Count)
        {
            string[] parts = _ingameResolutionOptions[SelectedIngameResolutionIndex].Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
            {
                iniSettings.IngameScreenWidth.Value = width;
                iniSettings.IngameScreenHeight.Value = height;

                // Calculate drag distance
                int dragDistance = iniSettings.CustomDragDistance.Value > 0
                    ? iniSettings.CustomDragDistance.Value
                    : width / ORIGINAL_RESOLUTION_WIDTH * DRAG_DISTANCE_DEFAULT;
                iniSettings.DragDistance.Value = dragDistance;
            }
        }

        // Save client resolution
        if (SelectedClientResolutionIndex >= 0 && SelectedClientResolutionIndex < _clientResolutionOptions.Count)
        {
            string[] parts = _clientResolutionOptions[SelectedClientResolutionIndex].Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
            {
                if (width != iniSettings.ClientResolutionX.Value || height != iniSettings.ClientResolutionY.Value)
                    restartRequired = true;

                iniSettings.ClientResolutionX.Value = width;
                iniSettings.ClientResolutionY.Value = height;
            }
        }

        DirectDrawWrapper? newSelectedRenderer = SelectedRendererIndex >= 0 && SelectedRendererIndex < _renderers.Count
            ? _renderers[SelectedRendererIndex]
            : null;
        bool isChangingRenderer = newSelectedRenderer != null && newSelectedRenderer != directDrawWrapperManager.SelectedRenderer;

        // Save windowed mode
        iniSettings.WindowedMode.Value = IsWindowedModeEnabled && !newSelectedRenderer.UsesCustomWindowedOption();
        iniSettings.BorderlessWindowedMode.Value = IsBorderlessWindowedModeEnabled && string.IsNullOrEmpty(newSelectedRenderer.BorderlessWindowedModeKey);

        if (newSelectedRenderer.UsesCustomWindowedOption())
        {
            // Save to renderer INI
            IniFile rendererSettingsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, newSelectedRenderer.ConfigFileName));
            rendererSettingsIni.SetBooleanValue(newSelectedRenderer.WindowedModeSection, newSelectedRenderer.WindowedModeKey, IsWindowedModeEnabled);

            if (!string.IsNullOrEmpty(newSelectedRenderer.BorderlessWindowedModeKey))
            {
                bool borderlessModeIniValue = IsBorderlessWindowedModeEnabled;
                if (newSelectedRenderer.IsBorderlessWindowedModeKeyReversed)
                    borderlessModeIniValue = !borderlessModeIniValue;

                rendererSettingsIni.SetBooleanValue(newSelectedRenderer.WindowedModeSection, newSelectedRenderer.BorderlessWindowedModeKey, borderlessModeIniValue);
            }

            rendererSettingsIni.WriteIniFile();
        }

        if (iniSettings.BorderlessWindowedClient.Value != IsBorderlessClientEnabled)
            restartRequired = true;

        iniSettings.BorderlessWindowedClient.Value = IsBorderlessClientEnabled;

        if (iniSettings.IntegerScaledClient.Value != IsIntegerScaledClientEnabled)
            restartRequired = true;

        iniSettings.IntegerScaledClient.Value = IsIntegerScaledClientEnabled;

        // Save theme
        if (SelectedThemeIndex >= 0 && SelectedThemeIndex < _themeNames.Count)
        {
            restartRequired = restartRequired || iniSettings.ClientTheme != _themeNames[SelectedThemeIndex];
            iniSettings.ClientTheme.Value = _themeNames[SelectedThemeIndex];
        }

        // Save translation
        if (SelectedTranslationIndex >= 0 && SelectedTranslationIndex < _translationLocaleCodes.Count)
        {
            bool updateTranslation = !iniSettings.Translation.ToString().Equals(_translationLocaleCodes[SelectedTranslationIndex], StringComparison.InvariantCultureIgnoreCase);
            restartRequired = restartRequired || updateTranslation;
            iniSettings.Translation.Value = _translationLocaleCodes[SelectedTranslationIndex];

            if (updateTranslation)
                iniSettings.TranslationGameFilesVersion.Value = string.Empty;
        }

        // Save back buffer
        if (ClientConfiguration.Instance.ClientGameType == ClientType.TS)
            iniSettings.BackBufferInVRAM.Value = !IsBackBufferStoredInVideoMemory;
        else
            iniSettings.BackBufferInVRAM.Value = IsBackBufferStoredInVideoMemory;

        // Save renderer
        if (newSelectedRenderer != null)
            directDrawWrapperManager.Save(newSelectedRenderer);

        // Copy language DLL for TS
        if (ClientConfiguration.Instance.ClientGameType == ClientType.TS && ClientConfiguration.Instance.CopyResolutionDependentLanguageDLL)
        {
            if (SelectedIngameResolutionIndex >= 0 && SelectedIngameResolutionIndex < _ingameResolutionOptions.Count)
            {
                string[] parts = _ingameResolutionOptions[SelectedIngameResolutionIndex].Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
                {
                    string languageDllDestinationPath = SafePath.CombineFilePath(ProgramConstants.GamePath, "Language.dll");
                    FileInfo fileInfo = SafePath.GetFile(languageDllDestinationPath);
                    if (fileInfo.Exists)
                    {
                        fileInfo.IsReadOnly = false;
                        fileInfo.Delete();
                    }

                    if (width >= 1024 && height >= 720)
                        File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, "Resources", "language_1024x720.dll"), languageDllDestinationPath);
                    else if (width >= 800 && height >= 600)
                        File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, "Resources", "language_800x600.dll"), languageDllDestinationPath);
                    else
                        File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, "Resources", "language_640x480.dll"), languageDllDestinationPath);
                }
            }
        }

        if (restartRequired)
            IsRestartRequired = true;

#if ISWINDOWS
        // Check for DirectDraw compatibility issues after renderer change
        if (isChangingRenderer && newSelectedRenderer != null && !newSelectedRenderer.IsDummy)
        {
            DirectDrawCompatibilityChecker.Examine(out bool requireFix, out bool requireAdmin, out IEnumerable<string> problematicExeNames);
            if (requireFix)
            {
                IsDirectDrawCompatFixRequired = true;
                DirectDrawCompatFixRequiresAdmin = requireAdmin;
            }
        }
#endif
    }

    [RelayCommand]
    private void DismissMessageBox()
    {
        IsMessageBoxVisible = false;
    }

    [RelayCommand]
    private void ApplyDirectDrawCompatFix()
    {
#if ISWINDOWS
        try
        {
            DirectDrawCompatibilityChecker.Fix();
        }
        catch (Exception ex)
        {
            Logger.Log("Failed to apply DirectDraw compatibility fix: " + ex.Message);
        }
#endif
        IsDirectDrawCompatFixRequired = false;
    }

    [RelayCommand]
    private void DismissDirectDrawCompatFix()
    {
        IsDirectDrawCompatFixRequired = false;
    }

    // --- Property change handlers ---

    partial void OnIsWindowedModeEnabledChanged(bool value)
    {
        IsBorderlessWindowedModeAllowed = value;
        if (!value)
            IsBorderlessWindowedModeEnabled = false;
    }

    partial void OnIsBorderlessClientEnabledChanged(bool value)
    {
        if (value)
        {
            string nativeRes = resolutionProvider.GetSafeFullScreenResolution();
            int nativeResIndex = _clientResolutionOptions.ToList().FindIndex(r => r == nativeRes);
            if (nativeResIndex > -1)
                SelectedClientResolutionIndex = nativeResIndex;
        }
        else
        {
            string bestRes = resolutionProvider.GetBestRecommendedResolution();
            int bestResIndex = _clientResolutionOptions.ToList().FindIndex(r => r == bestRes);
            if (bestResIndex > -1)
                SelectedClientResolutionIndex = bestResIndex;
        }
    }

    // --- Helpers ---

    private void PopulateOptions()
    {
        // Populate detail level options
        _detailLevelOptions.Add("Low".L10N("Client:DTAConfig:DetailLevelLow"));
        _detailLevelOptions.Add("Medium".L10N("Client:DTAConfig:DetailLevelMedium"));
        _detailLevelOptions.Add("High".L10N("Client:DTAConfig:DetailLevelHigh"));

        // Populate ingame resolutions via service
        foreach (string res in resolutionProvider.GetIngameResolutions())
            _ingameResolutionOptions.Add(res);

        // Populate client resolutions via service
        foreach (string res in resolutionProvider.GetClientResolutions())
            _clientResolutionOptions.Add(res);

        // Populate renderers
        foreach (var renderer in directDrawWrapperManager.GetRenderers(ClientConfiguration.Instance.GetOperatingSystemVersion()))
        {
            _rendererOptions.Add(renderer.UIName);
            _renderers.Add(renderer);
        }

        // Populate themes
        int themeCount = ClientConfiguration.Instance.ThemeCount;
        for (int i = 0; i < themeCount; i++)
        {
            string themeName = ClientConfiguration.Instance.GetThemeInfoFromIndex(i).Name;
            string displayName = themeName.L10N($"INI:Themes:{themeName}");
            _themeOptions.Add(displayName);
            _themeNames.Add(themeName);
        }

        // Populate translations
        foreach (var (localeCode, name) in Translation.GetTranslations())
        {
            _translationOptions.Add(name);
            _translationLocaleCodes.Add(localeCode);
        }
    }

    private void CheckCompatibilityFixes()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        if (ClientConfiguration.Instance.ClientGameType != ClientType.TS)
            return;

        try
        {
            using var regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Tiberian Sun Client");
            if (regKey == null)
                return;

            object tsCompatFixValue = regKey.GetValue("TSCompatFixInstalled", "No");
            gameCompatFixInstalled = (string)tsCompatFixValue == "Yes";

            object fsCompatFixValue = regKey.GetValue("FSCompatFixInstalled", "No");
            finalSunCompatFixInstalled = (string)fsCompatFixValue == "Yes";

            IsGameCompatFixAvailable = gameCompatFixInstalled;
            IsFinalSunCompatFixAvailable = finalSunCompatFixInstalled;
        }
        catch (Exception ex)
        {
            Logger.Log("Error checking compatibility fixes: " + ex.Message);
        }
    }

    private void ShowMessageBox(string title, string message)
    {
        MessageBoxTitle = title;
        MessageBoxMessage = message;
        IsMessageBoxVisible = true;
    }
}



