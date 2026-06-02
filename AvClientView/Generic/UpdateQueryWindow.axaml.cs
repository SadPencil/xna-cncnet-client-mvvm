using AvClientMvvmContract.Generic;

using Avalonia.Controls;

namespace AvClientView.Generic;

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
