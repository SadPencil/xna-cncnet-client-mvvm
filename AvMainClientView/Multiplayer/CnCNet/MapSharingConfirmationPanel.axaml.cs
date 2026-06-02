using AvMainClientMvvmContract.Multiplayer.CnCNet;

using Avalonia.Controls;

namespace AvMainClientView.Multiplayer.CnCNet;

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
