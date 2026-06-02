using AvClientMvvmContract.Multiplayer.CnCNet;

using Avalonia.Controls;

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
