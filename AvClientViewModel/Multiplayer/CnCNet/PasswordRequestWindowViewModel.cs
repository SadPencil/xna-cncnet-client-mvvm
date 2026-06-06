using System;

using AvClientMvvmContract.Multiplayer.CnCNet;

using AvClientViewModel.Domain.Multiplayer.CnCNet;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvClientViewModel.Multiplayer.CnCNet;


/// <summary>
/// ViewModel for the password request window.
/// Contains all business logic from PasswordRequestWindow.cs except XNA UI rendering.
/// </summary>
public partial class PasswordRequestWindowViewModel : ObservableObject, IPasswordRequestWindowViewModel
{
    private HostedCnCNetGame? hostedGame;

    // --- Observable state ---

    [ObservableProperty]
    public partial string GameName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HostName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

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

        IsVisible = false;
        onPasswordEntered?.Invoke(new PasswordEventArgs(Password, hostedGame!));
        Password = string.Empty;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsVisible = false;
        Password = string.Empty;
    }

    // --- Public methods ---

    public void Open(HostedCnCNetGame hostedGame)
    {
        this.hostedGame = hostedGame;
        GameName = hostedGame.RoomName;
        HostName = hostedGame.HostName;
        Password = string.Empty;
        IsVisible = true;
    }
}

