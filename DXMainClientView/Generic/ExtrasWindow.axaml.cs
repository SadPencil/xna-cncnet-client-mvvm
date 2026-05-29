using Avalonia.Controls;
using DXMainClientViewModel.Generic;

namespace DXMainClientView.Generic;

public partial class ExtrasWindow : UserControl, IExtrasWindowView
{
    public ExtrasWindow()
    {
        InitializeComponent();
    }

    public IExtrasWindowViewModel? ViewModel
    {
        get => DataContext as IExtrasWindowViewModel;
        set => DataContext = value;
    }
}
