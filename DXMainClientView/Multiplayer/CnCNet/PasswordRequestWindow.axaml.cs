using DXMainClientMVVMContract.Multiplayer.CnCNet;
using Avalonia.Controls;

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
