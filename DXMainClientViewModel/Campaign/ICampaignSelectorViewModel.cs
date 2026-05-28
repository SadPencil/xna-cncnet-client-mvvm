using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Campaign;

public interface ICampaignSelectorViewModel : INotifyPropertyChanged
{
    IReadOnlyList<CampaignListItem> CampaignListItems { get; }
    int SelectedCampaignIndex { get; set; }
    string MissionDescriptionText { get; }
    string? MissionPreviewImagePath { get; }
    bool IsMissionPreviewPanelVisible { get; }
    bool IsReturnButtonVisible { get; }
    bool IsControlsEnabled { get; }
    IReadOnlyList<string> DifficultyNames { get; }
    int SelectedDifficultyIndex { get; set; }
    bool CanLaunchCampaign { get; }

    ICheaterWindowViewModel CheaterWindow { get; }

    IRelayCommand LaunchCampaignCommand { get; }
    IRelayCommand ReturnCommand { get; }
    IRelayCommand CancelCommand { get; }
    IRelayCommand RefreshCommand { get; }

    void Initialize();
}
