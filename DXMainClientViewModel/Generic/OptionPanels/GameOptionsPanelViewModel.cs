
using System;

using ClientCore;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer.CnCNet;

using Rampastring.Tools;

namespace DXMainClientViewModel.Generic.OptionPanels;

/// <summary>
/// ViewModel for the game options panel.
/// Contains all business logic from GameOptionsPanel.cs except XNA UI rendering.
/// </summary>
public partial class GameOptionsPanelViewModel : ObservableObject, IGameOptionsPanelViewModel
{
    private const int MAX_SCROLL_RATE = 6;
    private const string TEXT_BACKGROUND_COLOR_TRANSPARENT = "0";
    private const string TEXT_BACKGROUND_COLOR_BLACK = "12";

    private readonly UserINISettings iniSettings;

    // --- Observable state ---

    [ObservableProperty]
    private int _scrollRate;

    [ObservableProperty]
    private bool _isScrollCoastingEnabled;

    [ObservableProperty]
    private bool _areTargetLinesEnabled;

    [ObservableProperty]
    private bool _areTooltipsEnabled;

    [ObservableProperty]
    private bool _areHiddenObjectsVisible;

    [ObservableProperty]
    private bool _isBlackChatBackgroundEnabled;

    [ObservableProperty]
    private bool _isUndeployWithAltEnabled;

    [ObservableProperty]
    private string _playerName = string.Empty;

    [ObservableProperty]
    private bool _showHotkeyConfiguration;

    // --- Constructor ---

    public GameOptionsPanelViewModel(UserINISettings iniSettings)
    {
        this.iniSettings = iniSettings;
    }

    // --- Commands ---

    [RelayCommand]
    private void OpenHotkeyConfiguration()
    {
        ShowHotkeyConfiguration = true;
    }

    [RelayCommand]
    private void LoadSettings()
    {
        int scrollRate = ReverseScrollRate(iniSettings.ScrollRate);
        if (scrollRate >= 0 && scrollRate <= MAX_SCROLL_RATE)
            ScrollRate = scrollRate;

        PlayerName = UserINISettings.Instance.PlayerName;

        // Load checkbox settings from INI
        var ini = iniSettings.SettingsIni;
        IsScrollCoastingEnabled = GetIniBool(ini, UserINISettings.OPTIONS, "ScrollMethod", true, true, "0", "1");
        AreTargetLinesEnabled = GetIniBool(ini, UserINISettings.OPTIONS, "UnitActionLines", true, false, "", "");
        AreTooltipsEnabled = GetIniBool(ini, UserINISettings.OPTIONS, "ToolTips", true, false, "", "");
        AreHiddenObjectsVisible = GetIniBool(ini, UserINISettings.OPTIONS, "ShowHidden", true, false, "", "");
        IsBlackChatBackgroundEnabled = GetIniBool(ini, UserINISettings.OPTIONS, "TextBackgroundColor", false, true, TEXT_BACKGROUND_COLOR_BLACK, TEXT_BACKGROUND_COLOR_TRANSPARENT);
        IsUndeployWithAltEnabled = GetIniBool(ini, UserINISettings.OPTIONS, "MoveToUndeploy", true, false, "", "");
    }

    [RelayCommand]
    private void SaveSettings()
    {
        iniSettings.ScrollRate.Value = ReverseScrollRate(ScrollRate);

        string validName = NameValidator.GetValidOfflineName(PlayerName);
        if (validName.Length > 0)
            iniSettings.PlayerName.Value = validName;

        // Save checkbox settings to INI
        var ini = iniSettings.SettingsIni;
        SetIniBool(ini, UserINISettings.OPTIONS, "ScrollMethod", IsScrollCoastingEnabled, true, "0", "1");
        SetIniBool(ini, UserINISettings.OPTIONS, "UnitActionLines", AreTargetLinesEnabled, false, "", "");
        SetIniBool(ini, UserINISettings.OPTIONS, "ToolTips", AreTooltipsEnabled, false, "", "");
        SetIniBool(ini, UserINISettings.OPTIONS, "ShowHidden", AreHiddenObjectsVisible, false, "", "");
        SetIniBool(ini, UserINISettings.OPTIONS, "TextBackgroundColor", IsBlackChatBackgroundEnabled, true, TEXT_BACKGROUND_COLOR_BLACK, TEXT_BACKGROUND_COLOR_TRANSPARENT);
        SetIniBool(ini, UserINISettings.OPTIONS, "MoveToUndeploy", IsUndeployWithAltEnabled, false, "", "");
    }

    // --- Helpers ---

    private int ReverseScrollRate(int scrollRate)
    {
        return Math.Abs(scrollRate - MAX_SCROLL_RATE);
    }

    private static bool GetIniBool(IniFile ini, string section, string key, bool defaultValue, bool writeSettingValue, string enabledValue, string disabledValue)
    {
        if (writeSettingValue)
        {
            string value = ini.GetStringValue(section, key, defaultValue ? enabledValue : disabledValue);
            return value == enabledValue;
        }
        else
        {
            return ini.GetBooleanValue(section, key, defaultValue);
        }
    }

    private static void SetIniBool(IniFile ini, string section, string key, bool value, bool writeSettingValue, string enabledValue, string disabledValue)
    {
        if (writeSettingValue)
        {
            ini.SetStringValue(section, key, value ? enabledValue : disabledValue);
        }
        else
        {
            ini.SetBooleanValue(section, key, value);
        }
    }
}

// checked
