using DXMainClientMVVMContract.Generic;

using Avalonia.Controls;

namespace DXMainClientView.Generic;

public partial class GameInProgressWindow : UserControl, IGameInProgressWindowView
{
    public GameInProgressWindow()
    {
        InitializeComponent();
    }

    public IGameInProgressWindowViewModel? ViewModel
    {
        get => DataContext as IGameInProgressWindowViewModel;
        set => DataContext = value;
    }
}
