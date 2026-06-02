using AvClientMvvmContract.Campaign;

namespace AvClientView.Campaign;

public interface ICampaignTagSelectorView
{
    ICampaignTagSelectorViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
