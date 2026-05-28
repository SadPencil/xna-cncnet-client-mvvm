using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Campaign;

public interface ICampaignSelectorViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> CampaignNames { get; }
    int SelectedCampaignIndex { get; set; }
    string SelectedCampaignName { get; }
    string SelectedCampaignDescription { get; }
    string MissionDescriptionText { get; }
    string? MissionPreviewImagePath { get; }
    bool IsMissionPreviewPanelVisible { get; }
    bool IsReturnButtonVisible { get; }
    bool IsControlsEnabled { get; }
    IReadOnlyList<string> DifficultyNames { get; }
    int SelectedDifficultyIndex { get; set; }
    bool CanLaunchCampaign { get; }
    bool IsCheaterWindowVisible { get; }

    ICheaterWindowViewModel CheaterWindow { get; }

    IReadOnlyCollection<Domain.Mission> AllMissions { get; }
    IReadOnlyDictionary<int, Domain.Mission> UniqueIDToMissions { get; }

    List<ICampaignCheckBoxOption> CheckBoxOptions { get; }
    List<ICampaignDropDownOption> DropDownOptions { get; }

    IRelayCommand LaunchCampaignCommand { get; }
    IRelayCommand ReturnCommand { get; }
    IRelayCommand CancelCommand { get; }
    IRelayCommand RefreshCommand { get; }
}
