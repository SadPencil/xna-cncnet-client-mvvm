using Avalonia.Controls;
using DXMainClientViewModel.Generic;

namespace DXMainClientView.Generic;

public partial class ManualUpdateQueryWindow : UserControl, IManualUpdateQueryWindowView
{
    public ManualUpdateQueryWindow()
    {
        InitializeComponent();
    }

    public IManualUpdateQueryWindowViewModel? ViewModel
    {
        get => DataContext as IManualUpdateQueryWindowViewModel;
        set => DataContext = value;
    }
}
