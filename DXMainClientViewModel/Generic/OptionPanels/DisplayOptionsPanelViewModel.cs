using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;
using ClientCore.I18N;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain;

namespace DXMainClientViewModel.Generic.OptionPanels;

/// <summary>
/// ViewModel for the display options panel.
/// Contains all business logic from DisplayOptionsPanel.cs except XNA UI rendering.
/// </summary>
public partial class DisplayOptionsPanelViewModel : ObservableObject, IDisplayOptionsPanelViewModel
{
    private readonly UserINISettings iniSettings;
    private readonly DirectDrawWrapperManager directDrawWrapperManager;

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
    private bool _isBackBufferStoredInVideoMemory;

    [ObservableProperty]
    private bool _isBorderlessClientEnabled;

    [ObservableProperty]
    private bool _isIntegerScaledClientEnabled;

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

    // --- Events ---

    public event EventHandler? GameCompatibilityFixRequested;
    public event EventHandler? MapEditorCompatibilityFixRequested;

    // --- Constructor ---

    public DisplayOptionsPanelViewModel(UserINISettings iniSettings, DirectDrawWrapperManager directDrawWrapperManager)
    {
        this.iniSettings = iniSettings;
        this.directDrawWrapperManager = directDrawWrapperManager;

        // Initialize detail level options
        _detailLevelOptions.Add("Low".L10N("Client:DTAConfig:DetailLevelLow"));
        _detailLevelOptions.Add("Medium".L10N("Client:DTAConfig:DetailLevelMedium"));
        _detailLevelOptions.Add("High".L10N("Client:DTAConfig:DetailLevelHigh"));
    }

    // --- Commands ---

    [RelayCommand]
    private void InstallGameCompatibilityFix()
    {
        GameCompatibilityFixRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void InstallMapEditorCompatibilityFix()
    {
        MapEditorCompatibilityFixRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void LoadSettings()
    {
        // Load renderer
        int rendererIndex = _rendererOptions.ToList().FindIndex(r => r == directDrawWrapperManager.SelectedRenderer.UIName);
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
        int themeIndex = _themeOptions.ToList().FindIndex(t => t == UserINISettings.Instance.ClientTheme);
        SelectedThemeIndex = themeIndex >= 0 ? themeIndex : 0;

        // Load translation
        foreach (string localeCode in new string[] { UserINISettings.Instance.Translation, Translation.GetDefaultTranslationLocaleCode(), ProgramConstants.HARDCODED_LOCALE_CODE })
        {
            int transIndex = _translationOptions.ToList().FindIndex(t => t == localeCode);
            if (transIndex >= 0)
            {
                SelectedTranslationIndex = transIndex;
                break;
            }
        }

        // Load windowed mode
        IsWindowedModeEnabled = UserINISettings.Instance.WindowedMode;
        IsBorderlessWindowedModeEnabled = UserINISettings.Instance.BorderlessWindowedMode;

        // Load back buffer
        if (ClientConfiguration.Instance.ClientGameType == ClientType.TS)
            IsBackBufferStoredInVideoMemory = !UserINISettings.Instance.BackBufferInVRAM;
        else
            IsBackBufferStoredInVideoMemory = UserINISettings.Instance.BackBufferInVRAM;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        iniSettings.DetailLevel.Value = SelectedDetailLevelIndex;

        // Save ingame resolution
        if (SelectedIngameResolutionIndex >= 0 && SelectedIngameResolutionIndex < _ingameResolutionOptions.Count)
        {
            string[] parts = _ingameResolutionOptions[SelectedIngameResolutionIndex].Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
            {
                iniSettings.IngameScreenWidth.Value = width;
                iniSettings.IngameScreenHeight.Value = height;
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

        iniSettings.WindowedMode.Value = IsWindowedModeEnabled;
        iniSettings.BorderlessWindowedMode.Value = IsBorderlessWindowedModeEnabled;
        iniSettings.BorderlessWindowedClient.Value = IsBorderlessClientEnabled;
        iniSettings.IntegerScaledClient.Value = IsIntegerScaledClientEnabled;

        // Save theme
        if (SelectedThemeIndex >= 0 && SelectedThemeIndex < _themeOptions.Count)
            iniSettings.ClientTheme.Value = _themeOptions[SelectedThemeIndex];

        // Save translation
        if (SelectedTranslationIndex >= 0 && SelectedTranslationIndex < _translationOptions.Count)
            iniSettings.Translation.Value = _translationOptions[SelectedTranslationIndex];

        // Save back buffer
        if (ClientConfiguration.Instance.ClientGameType == ClientType.TS)
            iniSettings.BackBufferInVRAM.Value = !IsBackBufferStoredInVideoMemory;
        else
            iniSettings.BackBufferInVRAM.Value = IsBackBufferStoredInVideoMemory;

        // Save renderer
        directDrawWrapperManager.Save(directDrawWrapperManager.SelectedRenderer);
    }

    // --- Property change handlers ---

    partial void OnIsWindowedModeEnabledChanged(bool value)
    {
        if (!value)
            IsBorderlessWindowedModeEnabled = false;
    }

    partial void OnIsBorderlessClientEnabledChanged(bool value)
    {
        // When borderless is enabled, the View should select the native resolution
        // This is handled by the View observing this property change
    }
}
