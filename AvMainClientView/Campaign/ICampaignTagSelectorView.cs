using AvMainClientMvvmContract.Campaign;

namespace AvMainClientView.Campaign;

public interface ICampaignTagSelectorView
{
    ICampaignTagSelectorViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
