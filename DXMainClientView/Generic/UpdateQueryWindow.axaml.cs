using DXMainClientMVVMContract.Generic;
using Avalonia.Controls;

namespace DXMainClientView.Generic;

public partial class UpdateQueryWindow : UserControl, IUpdateQueryWindowView
{
    public UpdateQueryWindow()
    {
        InitializeComponent();
    }

    public IUpdateQueryWindowViewModel? ViewModel
    {
        get => DataContext as IUpdateQueryWindowViewModel;
        set => DataContext = value;
    }
}
