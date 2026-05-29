using Avalonia.Controls;
using DXMainClientViewModel.Generic;

namespace DXMainClientView.Generic;

public partial class OptionsWindow : UserControl, IOptionsWindowView
{
    public OptionsWindow()
    {
        InitializeComponent();
    }

    public IOptionsWindowViewModel? ViewModel
    {
        get => DataContext as IOptionsWindowViewModel;
        set => DataContext = value;
    }
}
