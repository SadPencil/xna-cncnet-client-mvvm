using System;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic;

public interface ITopBarViewModel : INotifyPropertyChanged
{
    string ConnectionStatusText { get; }
    string PlayerCountText { get; }
    bool IsPlayerCountVisible { get; }
    string PlayerCountLabel { get; }
    bool AreSwitchButtonsClickable { get; }
    bool IsOptionsButtonClickable { get; }
    bool IsLogoutButtonClickable { get; }
    bool IsLanMode { get; }
    SwitchType LastSwitchType { get; }
    string MainButtonText { get; }

    IRelayCommand SwitchToPrimaryCommand { get; }
    IRelayCommand SwitchToSecondaryCommand { get; }
    IRelayCommand SwitchToTertiaryCommand { get; }
    IRelayCommand OpenOptionsCommand { get; }
    IRelayCommand LogoutCommand { get; }

    /// <summary>
    /// Raised when the top bar should animate down.
    /// </summary>
    event Action? BringDownRequested;

    /// <summary>
    /// Raised when logout is performed. View should handle navigation.
    /// </summary>
    event Action? LogoutPerformed;

    void Initialize();

    /// <summary>
    /// Sets LAN mode on or off.
    /// </summary>
    void SetLanMode(bool lanMode);

    /// <summary>
    /// Sets whether switch buttons are clickable.
    /// </summary>
    void SetSwitchButtonsClickable(bool clickable);

    /// <summary>
    /// Sets whether the options button is clickable.
    /// </summary>
    void SetOptionsButtonClickable(bool clickable);

    /// <summary>
    /// Sets the main button text.
    /// </summary>
    void SetMainButtonText(string text);

    /// <summary>
    /// Called when the options window enabled state changes.
    /// </summary>
    void OnOptionsWindowEnabledChanged(bool isEnabled);

    /// <summary>
    /// Cleans up resources.
    /// </summary>
    void Clean();
}

/// <summary>
/// Represents the type of view switch.
/// </summary>
public enum SwitchType
{
    PRIMARY,
    SECONDARY
}
