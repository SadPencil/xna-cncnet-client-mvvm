using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Generic;

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
    bool IsMusicPlaying { get; }

    // Dialog state
    bool IsMessageBoxVisible { get; }
    string MessageBoxTitle { get; }
    string MessageBoxMessage { get; }
    bool IsYesNoDialogVisible { get; }
    string YesNoDialogTitle { get; }
    string YesNoDialogMessage { get; }

    // Navigation state
    MainMenuPanel ActivePanel { get; }
    bool IsLanMode { get; }

    // Child window overlay visibility
    bool IsOptionsOverlayVisible { get; }
    bool IsExtrasOverlayVisible { get; }
    bool IsStatisticsOverlayVisible { get; }
    bool IsGameLoadingOverlayVisible { get; }

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
    IRelayCommand UpdateStatusCommand { get; }
    IRelayCommand OpenVersionCommand { get; }
    IRelayCommand DeclineUpdateCommand { get; }
    IRelayCommand AcceptUpdateCommand { get; }
    IRelayCommand ForceUpdateCommandCommand { get; }
    IRelayCommand DismissMessageBoxCommand { get; }
    IRelayCommand YesNoDialogYesCommand { get; }
    IRelayCommand YesNoDialogNoCommand { get; }
}

/// <summary>
/// Represents the active panel in the main menu.
/// </summary>
public enum MainMenuPanel
{
    PRIMARY,
    SECONDARY
}
