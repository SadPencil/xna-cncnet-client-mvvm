using DXMainClientMvvmContract.Multiplayer.CnCNet;

using Avalonia.Controls;

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
