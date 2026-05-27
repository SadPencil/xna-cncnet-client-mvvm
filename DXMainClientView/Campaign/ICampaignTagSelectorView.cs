#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ICampaignTagSelectorView
{
    ICampaignTagSelectorViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
