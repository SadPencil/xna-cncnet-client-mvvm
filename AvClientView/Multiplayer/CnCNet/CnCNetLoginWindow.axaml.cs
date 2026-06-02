using AvClientMvvmContract.Multiplayer.CnCNet;

using Avalonia.Controls;

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
