using Avalonia.Controls;

using AvClientMvvmContract.Campaign;

namespace AvClientView.Campaign;

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
