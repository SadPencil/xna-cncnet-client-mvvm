using AvMainClientMvvmContract.Campaign;

using Avalonia.Controls;

namespace AvMainClientView.Campaign;

public partial class CheaterWindow : Window, ICheaterWindowView
{
    public CheaterWindow()
    {
        InitializeComponent();
    }

    public ICheaterWindowViewModel? ViewModel
    {
        get => DataContext as ICheaterWindowViewModel;
        set => DataContext = value;
    }

    void ICheaterWindowView.Show()
    {
        this.Show();
    }

    void ICheaterWindowView.Hide()
    {
        Close();
    }
}
