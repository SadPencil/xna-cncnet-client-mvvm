using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using DXMainClientViewModel.Generic;

namespace DXMainClientView.Generic;

public partial class MainMenu : UserControl
{
    public MainMenu()
    {
        InitializeComponent();
    }

    public IMainMenuViewModel? ViewModel
    {
        get => DataContext as IMainMenuViewModel;
        set => DataContext = value;
    }

    /// <summary>
    /// Sets the TopBar's ViewModel. Must be called before the control is shown.
    /// </summary>
    public void SetTopBarViewModel(ITopBarViewModel topBarViewModel)
    {
        topBar.ViewModel = topBarViewModel;
        topBarViewModel.PropertyChanged += OnTopBarPropertyChanged;
        UpdatePlayerCountVisibility(topBarViewModel.IsPlayerCountVisible);
    }

    private void OnTopBarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ITopBarViewModel.IsPlayerCountVisible))
        {
            var topBarVM = (ITopBarViewModel)sender!;
            Dispatcher.UIThread.Post(() => UpdatePlayerCountVisibility(topBarVM.IsPlayerCountVisible));
        }
    }

    private void UpdatePlayerCountVisibility(bool isVisible)
    {
        if (lblCnCNetStatus != null)
            lblCnCNetStatus.IsVisible = isVisible;
        if (lblCnCNetPlayerCount != null)
            lblCnCNetPlayerCount.IsVisible = isVisible;
    }
}
