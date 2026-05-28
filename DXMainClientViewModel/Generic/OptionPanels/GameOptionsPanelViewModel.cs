using System;

using ClientCore;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer.CnCNet;

namespace DXMainClientViewModel.Generic.OptionPanels;

/// <summary>
/// ViewModel for the game options panel.
/// Contains all business logic from GameOptionsPanel.cs except XNA UI rendering.
/// </summary>
public partial class GameOptionsPanelViewModel : ObservableObject, IGameOptionsPanelViewModel
{
    private const int MAX_SCROLL_RATE = 6;

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

    // --- Events ---

    public event EventHandler? HotkeyConfigurationRequested;

    // --- Constructor ---

    public GameOptionsPanelViewModel(UserINISettings iniSettings)
    {
        this.iniSettings = iniSettings;
    }

    // --- Commands ---

    [RelayCommand]
    private void OpenHotkeyConfiguration()
    {
        HotkeyConfigurationRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void LoadSettings()
    {
        ScrollRate = ReverseScrollRate(iniSettings.ScrollRate);
        PlayerName = UserINISettings.Instance.PlayerName;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        iniSettings.ScrollRate.Value = ReverseScrollRate(ScrollRate);

        string validName = NameValidator.GetValidOfflineName(PlayerName);
        if (validName.Length > 0)
            iniSettings.PlayerName.Value = validName;
    }

    // --- Helpers ---

    private int ReverseScrollRate(int scrollRate)
    {
        return Math.Abs(scrollRate - MAX_SCROLL_RATE);
    }
}
