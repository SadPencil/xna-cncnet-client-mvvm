using Avalonia.Controls;
using DXMainClientViewModel.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public partial class TunnelSelectionWindow : UserControl, ITunnelSelectionWindowView
{
    public TunnelSelectionWindow()
    {
        InitializeComponent();
    }

    public ITunnelSelectionWindowViewModel? ViewModel
    {
        get => DataContext as ITunnelSelectionWindowViewModel;
        set => DataContext = value;
    }
}
