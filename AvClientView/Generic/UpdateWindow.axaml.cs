using Avalonia.Controls;

using AvClientMvvmContract.Generic;

namespace AvClientView.Generic;

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
