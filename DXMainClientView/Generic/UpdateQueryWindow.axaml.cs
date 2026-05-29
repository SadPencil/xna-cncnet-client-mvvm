using Avalonia.Controls;
using DXMainClientViewModel.Generic;

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
