using Avalonia.Controls;
using DXMainClientViewModel.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public partial class MapSharingConfirmationPanel : UserControl, IMapSharingConfirmationPanelView
{
    public MapSharingConfirmationPanel()
    {
        InitializeComponent();
    }

    public IMapSharingConfirmationPanelViewModel? ViewModel
    {
        get => DataContext as IMapSharingConfirmationPanelViewModel;
        set => DataContext = value;
    }
}
