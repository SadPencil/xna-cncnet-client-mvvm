using Avalonia.Controls;
using DXMainClientViewModel.Campaign;

namespace DXMainClientView.Campaign;

public partial class CampaignSelector : Window, ICampaignSelectorView
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

    void ICampaignSelectorView.Show()
    {
        this.Show();
    }

    void ICampaignSelectorView.Hide()
    {
        Close();
    }
}
