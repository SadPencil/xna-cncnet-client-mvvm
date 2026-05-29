using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer.CnCNet;

namespace DXMainClientViewModel.Multiplayer.CnCNet;


/// <summary>
/// ViewModel for the password request window.
/// Contains all business logic from PasswordRequestWindow.cs except XNA UI rendering.
/// </summary>
public partial class PasswordRequestWindowViewModel : ObservableObject, IPasswordRequestWindowViewModel
{
    private HostedCnCNetGame? hostedGame;

    // --- Observable state ---

    [ObservableProperty]
    private string _gameName = string.Empty;

    [ObservableProperty]
    private string _hostName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isWindowVisible;

    private readonly Action<PasswordEventArgs>? onPasswordEntered;

    // --- Constructor ---

    public PasswordRequestWindowViewModel(Action<PasswordEventArgs>? onPasswordEntered = null)
    {
        this.onPasswordEntered = onPasswordEntered;
    }

    // --- Commands ---

    [RelayCommand]
    private void SubmitPassword()
    {
        if (string.IsNullOrEmpty(Password))
            return;

        IsWindowVisible = false;
        onPasswordEntered?.Invoke(new PasswordEventArgs(Password, hostedGame!));
        Password = string.Empty;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsWindowVisible = false;
        Password = string.Empty;
    }

    // --- Public methods ---

    public void Open(HostedCnCNetGame hostedGame)
    {
        this.hostedGame = hostedGame;
        GameName = hostedGame.RoomName;
        HostName = hostedGame.HostName;
        Password = string.Empty;
        IsWindowVisible = true;
    }
}

