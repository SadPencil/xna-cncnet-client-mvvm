using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic;

public interface IMainMenuViewModel : INotifyPropertyChanged
{
    bool IsUpdateNotificationVisible { get; }
    string UpdateNotificationText { get; }
    bool IsMapEditorButtonVisible { get; }
    bool IsStatisticsButtonVisible { get; }

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
    IAsyncRelayCommand CheckForUpdatesCommand { get; }

    void Initialize();
}
