using Avalonia.Controls;
using DXMainClientViewModel.Generic;

namespace DXMainClientView.Generic;

public partial class UpdateWindow : UserControl, IUpdateWindowView
{
    public UpdateWindow()
    {
        InitializeComponent();
    }

    public IUpdateWindowViewModel? ViewModel
    {
        get => DataContext as IUpdateWindowViewModel;
        set => DataContext = value;
    }
}
