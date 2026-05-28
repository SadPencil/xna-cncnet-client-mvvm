using System;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic;

public interface IMainMenuViewModel : INotifyPropertyChanged
{
    string VersionText { get; }
    string UpdateStatusText { get; }
    bool IsUpdateStatusEnabled { get; }
    bool IsUpdateStatusUnderlined { get; }
    string CnCNetPlayerCountText { get; }
    bool AreButtonsEnabled { get; }
    bool IsUpdateNotificationVisible { get; }
    string UpdateNotificationText { get; }
    bool IsMapEditorButtonVisible { get; }
    bool IsStatisticsButtonVisible { get; }
    bool ShowVersionInfo { get; }

    IRelayCommand StartCampaignCommand { get; }
    IRelayCommand ContinueCampaignCommand { get; }
    IRelayCommand LoadGameCommand { get; }
    IRelayCommand StartSkirmishCommand { get; }
    IRelayCommand JoinCnCNetCommand { get; }
    IRelayCommand HostLANGameCommand { get; }
    IRelayCommand OpenOptionsCommand { get; }
    IRelayCommand OpenMapEditorCommand { get; }
    IRelayCommand OpenStatisticsCommand { get; }
    IRelayCommand OpenCreditsCommand { get; }
    IRelayCommand OpenExtrasCommand { get; }
    IRelayCommand ExitCommand { get; }
    IRelayCommand CheckForUpdatesCommand { get; }

    /// <summary>
    /// Raised when the client should exit.
    /// </summary>
    event Action ExitRequested;

    /// <summary>
    /// Raised when music should be stopped (game process starting).
    /// </summary>
    event Action MusicStopRequested;

    /// <summary>
    /// Raised when music should be played (returning to main menu).
    /// </summary>
    event Action MusicPlayRequested;

    /// <summary>
    /// Raised when music fade-out should start (exit button).
    /// </summary>
    event Action MusicFadeOutRequested;

    /// <summary>
    /// Raised when a message box needs to be shown.
    /// </summary>
    event Action<string, string> MessageBoxRequested;

    /// <summary>
    /// Raised when a yes/no dialog needs to be shown. The callback receives true for yes, false for no.
    /// </summary>
    event Action<string, string, Action<bool>> YesNoDialogRequested;

    /// <summary>
    /// Raised when the options window should be opened.
    /// </summary>
    event Action OptionsWindowOpenRequested;

    /// <summary>
    /// Raised when the options window should switch to custom components panel.
    /// </summary>
    event Action OptionsWindowCustomComponentsRequested;

    /// <summary>
    /// Raised when LAN mode should be set.
    /// </summary>
    event Action<bool> LanModeChanged;

    /// <summary>
    /// Raised when CnCNet connection should be initiated.
    /// </summary>
    event Action CnCNetConnectRequested;

    /// <summary>
    /// Raised when CnCNet disconnection is needed.
    /// </summary>
    event Action CnCNetDisconnectRequested;

    /// <summary>
    /// Raised when the secondary view should be shown (CnCNet lobby).
    /// </summary>
    event Action SwitchToSecondaryRequested;

    /// <summary>
    /// Raised when the primary view should be shown (main menu).
    /// </summary>
    event Action SwitchToPrimaryRequested;

    void Initialize();

    /// <summary>
    /// Called when returning from skirmish lobby.
    /// </summary>
    void OnSkirmishLobbyExited();

    /// <summary>
    /// Called when returning from LAN lobby.
    /// </summary>
    void OnLanLobbyExited();

    /// <summary>
    /// Called when the game process has exited.
    /// </summary>
    void OnGameProcessExited();

    /// <summary>
    /// Called when options window is closed (disabled).
    /// </summary>
    void OnOptionsWindowClosed();

    /// <summary>
    /// Called when the main menu is switched on (becomes visible).
    /// </summary>
    void SwitchOn();

    /// <summary>
    /// Called when the main menu is switched off (becomes hidden).
    /// </summary>
    void SwitchOff();

    /// <summary>
    /// Cleans up resources. Called when the game is closing.
    /// </summary>
    void Clean();
}
