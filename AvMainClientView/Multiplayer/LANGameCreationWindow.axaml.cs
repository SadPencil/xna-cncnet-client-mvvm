using AvMainClientMvvmContract.Multiplayer;

using Avalonia.Controls;

namespace AvMainClientView.Multiplayer;

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
