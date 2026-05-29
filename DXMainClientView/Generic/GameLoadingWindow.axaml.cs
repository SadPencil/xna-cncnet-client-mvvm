using Avalonia.Controls;
using DXMainClientViewModel.Generic;

namespace DXMainClientView.Generic;

public partial class GameLoadingWindow : UserControl, IGameLoadingWindowView
{
    public GameLoadingWindow()
    {
        InitializeComponent();
    }

    public IGameLoadingWindowViewModel? ViewModel
    {
        get => DataContext as IGameLoadingWindowViewModel;
        set => DataContext = value;
    }
}
