using Avalonia.Controls;

using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

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
