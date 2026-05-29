using Avalonia.Controls;
using DXMainClientViewModel.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public partial class GameCreationWindow : UserControl, IGameCreationWindowView
{
    public GameCreationWindow()
    {
        InitializeComponent();
    }

    public IGameCreationWindowViewModel? ViewModel
    {
        get => DataContext as IGameCreationWindowViewModel;
        set => DataContext = value;
    }
}
