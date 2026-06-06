using System;

using AvClientMvvmContract.Generic;

using ClientCore.Settings;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Generic;

/// <summary>
/// ViewModel for the MainWindow. Determines effective WindowState:
///   1. Game running + MinimizeWindowsOnGameStart → Minimized
///   2. BorderlessWindowedClient → FullScreen
///   3. Otherwise → user preference (Normal or Maximized from TwoWay binding)
/// </summary>
public partial class MainWindowViewModel : ObservableObject, IMainWindowViewModel
{
    private readonly IGameInProgressWindowViewModel gameInProgressVM;

    private bool _isUpdatingState;
    private WindowState _lastUserState = WindowState.Normal;

    [ObservableProperty]
    private WindowState windowState;

    public MainWindowViewModel(IGameInProgressWindowViewModel gameInProgressVM)
    {
        this.gameInProgressVM = gameInProgressVM;

        ApplyEffectiveState();

        gameInProgressVM.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IGameInProgressWindowViewModel.IsGameInProgress))
                ApplyEffectiveState();
        };
    }

    partial void OnWindowStateChanged(WindowState value)
    {
        if (_isUpdatingState)
            return;

        if (value == WindowState.Normal || value == WindowState.Maximized)
            _lastUserState = value;
    }

    private void ApplyEffectiveState()
    {
        _isUpdatingState = true;
        try
        {
            if (gameInProgressVM.IsGameInProgress
                && UserINISettings.Instance.MinimizeWindowsOnGameStart)
            {
                WindowState = WindowState.Minimized;
            }
            else if (UserINISettings.Instance.BorderlessWindowedClient)
            {
                WindowState = WindowState.FullScreen;
            }
            else
            {
                WindowState = _lastUserState;
            }
        }
        finally
        {
            _isUpdatingState = false;
        }
    }
}
