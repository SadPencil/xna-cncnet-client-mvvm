using AvMainClientMvvmContract.Generic;

using Avalonia.Controls;

namespace AvMainClientView.Generic;

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
