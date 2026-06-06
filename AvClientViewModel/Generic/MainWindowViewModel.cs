using System;

using AvClientMvvmContract.Generic;

using ClientCore.Settings;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Generic;

/// <summary>
/// ViewModel for the MainWindow.
/// Computes effective WindowState from BorderlessWindowedClient (FullScreen)
/// and game-in-progress state (Minimized), with minimize taking priority.
/// </summary>
public partial class MainWindowViewModel : ObservableObject, IMainWindowViewModel
{
    private readonly IGameInProgressWindowViewModel gameInProgressVM;

    [ObservableProperty]
    private WindowState windowState;

    public MainWindowViewModel(IGameInProgressWindowViewModel gameInProgressVM)
    {
        this.gameInProgressVM = gameInProgressVM;

        WindowState = ComputeEffectiveState();

        gameInProgressVM.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IGameInProgressWindowViewModel.WindowState))
                WindowState = ComputeEffectiveState();
        };
    }

    private WindowState ComputeEffectiveState()
    {
        bool borderless = UserINISettings.Instance.BorderlessWindowedClient;

        if (gameInProgressVM.WindowState == WindowState.Minimized)
            return WindowState.Minimized;

        return borderless ? WindowState.FullScreen : WindowState.Normal;
    }
}
