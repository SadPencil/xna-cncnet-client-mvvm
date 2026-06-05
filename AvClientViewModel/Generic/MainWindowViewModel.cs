using System;

using AvClientMvvmContract.Generic;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Generic;

/// <summary>
/// ViewModel for the MainWindow.
/// Observes child ViewModels and exposes state the MainWindow needs to bind to.
/// </summary>
public partial class MainWindowViewModel : ObservableObject, IMainWindowViewModel
{
    private readonly IGameInProgressWindowViewModel gameInProgressVM;

    [ObservableProperty]
    private WindowState windowState = WindowState.Normal;

    public MainWindowViewModel(IGameInProgressWindowViewModel gameInProgressVM)
    {
        this.gameInProgressVM = gameInProgressVM;

        gameInProgressVM.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IGameInProgressWindowViewModel.WindowState))
                WindowState = gameInProgressVM.WindowState;
        };
    }
}
