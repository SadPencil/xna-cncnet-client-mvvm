using Avalonia.Controls;

using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

public partial class CnCNetLoginWindow : UserControl, ICnCNetLoginWindowView
{
    public CnCNetLoginWindow()
    {
        InitializeComponent();
    }

    public ICnCNetLoginWindowViewModel? ViewModel
    {
        get => DataContext as ICnCNetLoginWindowViewModel;
        set => DataContext = value;
    }
}
