using Avalonia.Controls;
using DXMainClientViewModel.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public partial class PasswordRequestWindow : UserControl, IPasswordRequestWindowView
{
    public PasswordRequestWindow()
    {
        InitializeComponent();
    }

    public IPasswordRequestWindowViewModel? ViewModel
    {
        get => DataContext as IPasswordRequestWindowViewModel;
        set => DataContext = value;
    }
}
