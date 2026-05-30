#nullable enable
using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMVVMContract.Campaign;

public interface ICampaignSelectorViewModel : INotifyPropertyChanged
{
    IReadOnlyList<ICampaignListItem> CampaignListItems { get; }
    int SelectedCampaignIndex { get; set; }
    string MissionDescriptionText { get; }
    string? MissionPreviewImagePath { get; }
    bool IsControlsEnabled { get; }
    bool IsVisible { get; }
    bool IsReturnButtonVisible { get; }

    // Mission preview paths (View uses these to render preview panel)
    string MissionPreviewFolder { get; }
    string DefaultMissionPreviewPath { get; }
    bool IsMissionPreviewEnabled { get; }

    IReadOnlyList<string> DifficultyNames { get; }
    int SelectedDifficultyIndex { get; set; }
    bool CanLaunchCampaign { get; }
    bool IsCheaterWindowVisible { get; }

    ICheaterWindowViewModel CheaterWindow { get; }

    IReadOnlyCollection<Domain.IMission> AllMissions { get; }
    IReadOnlyDictionary<int, Domain.IMission> UniqueIDToMissions { get; }

    List<ICampaignCheckBoxOption> CheckBoxOptions { get; }
    List<ICampaignDropDownOption> DropDownOptions { get; }
    List<IUserSetting> UserSettings { get; }

    IRelayCommand LaunchCampaignCommand { get; }
    IRelayCommand ReturnCommand { get; }
    IRelayCommand CancelCommand { get; }
    IRelayCommand RefreshCommand { get; }
}
