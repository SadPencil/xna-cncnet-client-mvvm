using AvClientMvvmContract.Multiplayer.CnCNet;

using System;

using ClientCore;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using AvClientViewModel.Domain.Multiplayer.CnCNet;

namespace AvClientViewModel.Multiplayer.CnCNet;


/// <summary>
/// ViewModel for the CnCNet login window.
/// Contains all business logic from CnCNetLoginWindow.cs except XNA UI rendering.
/// </summary>
public partial class CnCNetLoginWindowViewModel : ObservableObject, ICnCNetLoginWindowViewModel
{
    private readonly UserINISettings iniSettings;

    // --- Observable state ---

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberPassword;

    [ObservableProperty]
    private bool _persistentMode;

    [ObservableProperty]
    private bool _autoConnect;

    [ObservableProperty]
    private bool _isAutoConnectAllowed;

    [ObservableProperty]
    private bool _isVisible;

    private readonly Action? onConnectRequested;
    private readonly Action? onCancelled;
    private readonly Action<string>? onValidationError;

    // --- Constructor ---

    public CnCNetLoginWindowViewModel(UserINISettings iniSettings, Action? onConnectRequested = null, Action? onCancelled = null, Action<string>? onValidationError = null)
    {
        this.iniSettings = iniSettings;
        this.onConnectRequested = onConnectRequested;
        this.onCancelled = onCancelled;
        this.onValidationError = onValidationError;
    }

    // --- Commands ---

    [RelayCommand]
    private void Connect()
    {
        NameValidationError validationError = NameValidator.IsNameValid(UserName, out string errorMessage);

        if (validationError != NameValidationError.None)
        {
            onValidationError?.Invoke(errorMessage);
            return;
        }

        ProgramConstants.PLAYERNAME = UserName;

        iniSettings.SkipConnectDialog.Value = RememberPassword;
        iniSettings.PersistentMode.Value = PersistentMode;
        iniSettings.AutomaticCnCNetLogin.Value = AutoConnect;
        iniSettings.PlayerName.Value = ProgramConstants.PLAYERNAME;

        iniSettings.SaveSettings();

        onConnectRequested?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        onCancelled?.Invoke();
    }

    // --- Public methods ---

    /// <summary>
    /// Loads settings from INI. If remember password is checked, auto-connects.
    /// </summary>
    public void LoadSettings()
    {
        AutoConnect = iniSettings.AutomaticCnCNetLogin;
        PersistentMode = iniSettings.PersistentMode;
        RememberPassword = iniSettings.SkipConnectDialog;
        UserName = iniSettings.PlayerName;

        CheckAutoConnectAllowance();

        if (RememberPassword)
            ConnectCommand.Execute(null);
    }

    // --- Property change handlers ---

    partial void OnRememberPasswordChanged(bool value) => CheckAutoConnectAllowance();
    partial void OnPersistentModeChanged(bool value) => CheckAutoConnectAllowance();

    // --- Helpers ---

    private void CheckAutoConnectAllowance()
    {
        IsAutoConnectAllowed = PersistentMode && RememberPassword;
        if (!IsAutoConnectAllowed)
            AutoConnect = false;
    }
}

