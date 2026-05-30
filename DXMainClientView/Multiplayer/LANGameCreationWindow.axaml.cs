using DXMainClientMVVMContract.Multiplayer;
using Avalonia.Controls;

namespace DXMainClientView.Multiplayer;

public partial class LANGameCreationWindow : UserControl, ILANGameCreationWindowView
{
    public LANGameCreationWindow()
    {
        InitializeComponent();
    }

    public ILANGameCreationWindowViewModel? ViewModel
    {
        get => DataContext as ILANGameCreationWindowViewModel;
        set => DataContext = value;
    }
}
