#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Campaign;

namespace DXMainClientView.Campaign;

public interface ICampaignSelectorView
{
    ICampaignSelectorViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
