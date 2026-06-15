using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

using AvClientMvvmContract.Domain;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Campaign;

public interface ICampaignTagSelectorViewModel : INotifyPropertyChanged
{
    ReadOnlyObservableCollection<string> CampaignTags { get; }
    int SelectedTagIndex { get; set; }
    string? SelectedTagName { get; }
    bool IsVisible { get; set; }
    IReadOnlyDictionary<int, IMission> UniqueIDToMissions { get; }
    IReadOnlyCollection<IMission> AllMissions { get; }

    IRelayCommand SelectTagCommand { get; }
    IRelayCommand ShowAllCampaignsCommand { get; }
    IRelayCommand CancelCommand { get; }
}
