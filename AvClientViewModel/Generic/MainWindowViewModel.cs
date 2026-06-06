using System;

using AvClientMvvmContract.Generic;

using ClientCore.Settings;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Generic;

/// <summary>
/// ViewModel for the MainWindow.
/// Computes effective WindowState from BorderlessWindowedClient (FullScreen)
/// and game-in-progress state (Minimized). Tracks user-initiated window state
/// changes (e.g. maximize) via TwoWay binding so that when a game exits and
/// the window is restored, it returns to the user's previous state.
/// Minimized always takes priority.
/// </summary>
public partial class MainWindowViewModel : ObservableObject, IMainWindowViewModel
{
    private readonly IGameInProgressWindowViewModel gameInProgressVM;

    private bool _isUpdatingState;
    private WindowState _lastUserState;

    [ObservableProperty]
    private WindowState windowState;

    public MainWindowViewModel(IGameInProgressWindowViewModel gameInProgressVM)
    {
        this.gameInProgressVM = gameInProgressVM;

        bool borderless = UserINISettings.Instance.BorderlessWindowedClient;
        _lastUserState = borderless ? WindowState.FullScreen : WindowState.Normal;

        ApplyEffectiveState();

        gameInProgressVM.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IGameInProgressWindowViewModel.WindowState))
                ApplyEffectiveState();
        };
    }

    /// <summary>
    /// Handles TwoWay binding feedback. When the user manually changes the
    /// window state (e.g. maximize via title bar), the View writes back
    /// through the binding. We only record Normal/Maximized as user preference;
    /// Minimized and FullScreen are ViewModel-driven.
    /// </summary>
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
            if (gameInProgressVM.WindowState == WindowState.Minimized)
                WindowState = WindowState.Minimized;
            else
                WindowState = _lastUserState;
        }
        finally
        {
            _isUpdatingState = false;
        }
    }
}
