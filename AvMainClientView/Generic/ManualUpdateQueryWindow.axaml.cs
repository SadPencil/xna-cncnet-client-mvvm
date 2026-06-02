using AvMainClientMvvmContract.Generic;

using Avalonia.Controls;

namespace AvMainClientView.Generic;

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
