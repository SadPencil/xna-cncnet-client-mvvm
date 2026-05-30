using DXMainClientMvvmContract.Multiplayer.CnCNet;

using Avalonia.Controls;

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
