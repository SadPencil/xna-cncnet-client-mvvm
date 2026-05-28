using Avalonia.Controls;
using DXMainClientViewModel.Generic;

namespace DXMainClientView.Generic;

public partial class LoadingScreen : Window, ILoadingScreenView
{
    public LoadingScreen()
    {
        InitializeComponent();
    }

    public ILoadingScreenViewModel? ViewModel
    {
        get => DataContext as ILoadingScreenViewModel;
        set => DataContext = value;
    }

    void ILoadingScreenView.Show()
    {
        this.Show();
    }

    void ILoadingScreenView.Hide()
    {
        Close();
    }
}
