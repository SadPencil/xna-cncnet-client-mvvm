#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ICampaignSelectorView
{
    ICampaignSelectorViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
