using Avalonia.Controls;
using DXMainClientViewModel.Campaign;

namespace DXMainClientView.Campaign;

public partial class CampaignSelector : UserControl
{
    public CampaignSelector()
    {
        InitializeComponent();
    }

    public ICampaignSelectorViewModel? ViewModel
    {
        get => DataContext as ICampaignSelectorViewModel;
        set => DataContext = value;
    }
}
