using DXMainClientViewModel.Campaign;

namespace DXMainClientView.Campaign;

public interface ICampaignTagSelectorView
{
    ICampaignTagSelectorViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
