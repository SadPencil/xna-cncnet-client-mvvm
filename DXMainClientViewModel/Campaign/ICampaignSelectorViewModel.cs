#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface ICampaignSelectorViewModel
{
    IReadOnlyList<string> CampaignNames { get; }
    int SelectedCampaignIndex { get; set; }
    string SelectedCampaignName { get; }
    string SelectedCampaignDescription { get; }
    IReadOnlyList<string> DifficultyNames { get; }
    int SelectedDifficultyIndex { get; set; }
    bool CanLaunchCampaign { get; }

    IRelayCommand LaunchCampaignCommand { get; }
    IRelayCommand ReturnCommand { get; }
    IRelayCommand CancelCommand { get; }
    IRelayCommand RefreshCommand { get; }

    void Initialize();
}
