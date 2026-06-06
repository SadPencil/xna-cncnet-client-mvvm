using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using AvClientMvvmContract.Generic.OptionPanels;
using AvClientMvvmContract.Messages;
using AvClientMvvmContract.ViewServices;

using AvClientViewModel.Domain;
using AvClientViewModel.Services;
using AvClientViewModel.Services.Resolutions;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;
using ClientCore.I18N;
using ClientCore.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using Rampastring.Tools;

using Serilog;

namespace AvClientViewModel.Generic.OptionPanels;

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
    private readonly DialogService dialogService;
    private readonly IProcessLifecycleService processLifecycleService;

    // --- State ---

    private bool gameCompatFixInstalled;
    private bool finalSunCompatFixInstalled;

    // --- Observable state ---

    [ObservableProperty]
    public partial int SelectedIngameResolutionIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedClientResolutionIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedDetailLevelIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedRendererIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedThemeIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedTranslationIndex { get; set; }

    [ObservableProperty]
    public partial bool IsWindowedModeEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsBorderlessWindowedModeEnabled { get; set; }

    /// <summary>
    /// Derived from IsWindowedModeEnabled - defined once, not repeated.
    /// </summary>
    public bool IsBorderlessWindowedModeAllowed => IsWindowedModeEnabled;

    [ObservableProperty]
    public partial bool IsBackBufferStoredInVideoMemory { get; set; }

    [ObservableProperty]
    public partial bool IsBorderlessClientEnabled { get; set; }

    /// <summary>
    /// Integer scaling is permanently disabled for the new client.
    /// </summary>
    public bool IsIntegerScaledClientAllowed => false;

    [ObservableProperty]
    public partial bool IsIntegerScaledClientEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsGameCompatFixAllowed { get; set; }

    [ObservableProperty]
    public partial bool IsFinalSunCompatFixAllowed { get; set; }

    [ObservableProperty]
    public partial bool IsRestartRequired { get; set; }

    [ObservableProperty]
    public partial bool IsDirectDrawCompatFixRequired { get; set; }

    [ObservableProperty]
    public partial bool DirectDrawCompatFixRequiresAdmin { get; set; }

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
        IResolutionProvider resolutionProvider,
        DialogService dialogService,
        IProcessLifecycleService processLifecycleService)
    {
        this.iniSettings = iniSettings;
        this.directDrawWrapperManager = directDrawWrapperManager;
        this.resolutionProvider = resolutionProvider;
        this.dialogService = dialogService;
        this.processLifecycleService = processLifecycleService;

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

            Log.Information("DTA/TI/TS Compatibility Fix succesfully uninstalled.");
            _ = dialogService.ShowOKDialog(
                   "Compatibility Fix Uninstalled".L10N("Client:DTAConfig:TSFixUninstallTitle"),
                   "The DTA/TI/TS Compatibility Fix has been succesfully uninstalled.".L10N("Client:DTAConfig:TSFixUninstallText"));

            using var regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
            using var subKey = regKey.CreateSubKey("Tiberian Sun Client");
            subKey.SetValue("TSCompatFixInstalled", "No");

            gameCompatFixInstalled = false;
            IsGameCompatFixAllowed = false;

            if (!finalSunCompatFixInstalled)
                IsFinalSunCompatFixAllowed = false;
        }
        catch (Exception ex)
        {
            Log.Warning("Uninstalling DTA/TI/TS Compatibility Fix failed. Error message: " + ex.ToString());
            _ = dialogService.ShowOKDialog(
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

            Log.Information("FinalSun Compatibility Fix succesfully uninstalled.");
            _ = dialogService.ShowOKDialog(
                  "Compatibility Fix Uninstalled".L10N("Client:DTAConfig:TSFinalSunFixUninstallTitle"),
                  "The FinalSun Compatibility Fix has been succesfully uninstalled.".L10N("Client:DTAConfig:TSFinalSunFixUninstallText"));

            finalSunCompatFixInstalled = false;
            IsFinalSunCompatFixAllowed = false;

            if (!gameCompatFixInstalled)
                IsGameCompatFixAllowed = false;
        }
        catch (Exception ex)
        {
            Log.Warning("Uninstalling FinalSun Compatibility Fix failed. Error message: " + ex.ToString());
            _ = dialogService.ShowOKDialog(
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
        IsIntegerScaledClientEnabled = false;

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

        // Borderless client no longer requires restart — MainWindowViewModel
        // picks up the INI change and re-evaluates WindowState dynamically.
        // If a restart becomes necessary later, add back:
        //   if (iniSettings.BorderlessWindowedClient.Value != IsBorderlessClientEnabled)
        //       restartRequired = true;
        iniSettings.BorderlessWindowedClient.Value = IsBorderlessClientEnabled;
        WeakReferenceMessenger.Default.Send(new BorderlessClientToggledMessage());

        iniSettings.IntegerScaledClient.Value = false;

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

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            // Since CheckAndPromptFixAsync might restart the client if admin rights are required, do this at the end.
            if (isChangingRenderer && newSelectedRenderer != null && !newSelectedRenderer.IsDummy)
                _ = DirectDrawCompatibilityChecker.CheckAndPromptFixAsync(dialogService, processLifecycleService);
    }

    [RelayCommand]
    private void ApplyDirectDrawCompatFix()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                DirectDrawCompatibilityChecker.Fix();
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to apply DirectDraw compatibility fix: " + ex.Message);
            }
        }

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
        OnPropertyChanged(nameof(IsBorderlessWindowedModeAllowed));
        if (!value)
            IsBorderlessWindowedModeEnabled = false;
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

            IsGameCompatFixAllowed = gameCompatFixInstalled;
            IsFinalSunCompatFixAllowed = finalSunCompatFixInstalled;
        }
        catch (Exception ex)
        {
            Log.Warning("Error checking compatibility fixes: " + ex.Message);
        }
    }
}



