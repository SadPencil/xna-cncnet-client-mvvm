using DXMainClientMvvmContract.Multiplayer.CnCNet;

using Avalonia.Controls;

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
