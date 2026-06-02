using AvMainClientMvvmContract.Multiplayer.CnCNet;

using Avalonia.Controls;

namespace AvMainClientView.Multiplayer.CnCNet;

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
