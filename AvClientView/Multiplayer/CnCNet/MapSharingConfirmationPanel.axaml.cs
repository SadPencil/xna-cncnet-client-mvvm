using AvClientMvvmContract.Multiplayer.CnCNet;

using Avalonia.Controls;

namespace AvClientView.Multiplayer.CnCNet;

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
