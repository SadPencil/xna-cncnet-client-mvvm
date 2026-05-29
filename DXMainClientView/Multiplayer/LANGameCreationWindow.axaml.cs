using Avalonia.Controls;
using DXMainClientViewModel.Multiplayer;

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
