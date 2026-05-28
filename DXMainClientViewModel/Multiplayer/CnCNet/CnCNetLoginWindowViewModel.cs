using System;
using System.Threading.Tasks;

using ClientCore;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer.CnCNet;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

// checked
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
    private bool _isWindowVisible;

    // --- Events ---

    public event EventHandler? ConnectRequested;
    public event EventHandler? Cancelled;
    public event EventHandler<string>? ValidationError;

    // --- Constructor ---

    public CnCNetLoginWindowViewModel(UserINISettings iniSettings)
    {
        this.iniSettings = iniSettings;
    }

    // --- Commands ---

    [RelayCommand]
    private async Task Connect()
    {
        NameValidationError validationError = NameValidator.IsNameValid(UserName, out string errorMessage);

        if (validationError != NameValidationError.None)
        {
            ValidationError?.Invoke(this, errorMessage);
            return;
        }

        ProgramConstants.PLAYERNAME = UserName;

        iniSettings.SkipConnectDialog.Value = RememberPassword;
        iniSettings.PersistentMode.Value = PersistentMode;
        iniSettings.AutomaticCnCNetLogin.Value = AutoConnect;
        iniSettings.PlayerName.Value = ProgramConstants.PLAYERNAME;

        iniSettings.SaveSettings();

        IsWindowVisible = false;
        ConnectRequested?.Invoke(this, EventArgs.Empty);

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsWindowVisible = false;
        Cancelled?.Invoke(this, EventArgs.Empty);
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
