#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface ICampaignTagSelectorViewModel
{
    IReadOnlyList<string> CampaignTags { get; }
    int SelectedTagIndex { get; set; }
    string? SelectedTagName { get; }

    IRelayCommand SelectTagCommand { get; }
    IRelayCommand ShowAllCampaignsCommand { get; }
    IRelayCommand CancelCommand { get; }

    void Initialize();
}
