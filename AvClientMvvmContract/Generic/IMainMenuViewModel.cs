using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Generic;

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

    // Child ViewModels
    ITopBarViewModel TopBarViewModel { get; }

    // Navigation state
    MainMenuPanel ActivePanel { get; }
    bool IsLanMode { get; }

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
}

/// <summary>
/// Represents the active panel in the main menu.
/// </summary>
public enum MainMenuPanel
{
    PRIMARY,
    SECONDARY
}
