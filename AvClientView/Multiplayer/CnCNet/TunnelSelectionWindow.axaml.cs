using Avalonia.Controls;

using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

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
