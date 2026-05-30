using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using DXMainClientMVVMContract.Domain;

namespace DXMainClientMVVMContract.Campaign;


public interface ICampaignTagSelectorViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> CampaignTags { get; }
    int SelectedTagIndex { get; set; }
    string? SelectedTagName { get; }
    bool IsVisible { get; set; }
    IReadOnlyDictionary<int, IMission> UniqueIDToMissions { get; }
    IReadOnlyCollection<IMission> AllMissions { get; }

    IRelayCommand SelectTagCommand { get; }
    IRelayCommand ShowAllCampaignsCommand { get; }
    IRelayCommand CancelCommand { get; }
}
