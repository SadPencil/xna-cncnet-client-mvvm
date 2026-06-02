using AvMainClientMvvmContract.Multiplayer.CnCNet;

using Avalonia.Controls;

namespace AvMainClientView.Multiplayer.CnCNet;

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
