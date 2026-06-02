using AvClientMvvmContract.Domain.Multiplayer;
using AvClientMvvmContract.Multiplayer.GameLobby;

using AvClientViewModel.Domain.Multiplayer;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// Observable wrapper for a game option checkbox.
/// </summary>
public class GameOptionCheckBox : ObservableObject, IGameOptionCheckBox
{
    public GameSessionSetting Setting { get; }

    public string Name => Setting.Name;

    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (SetProperty(ref _isChecked, value))
                Setting.Value = value ? 1 : 0;
        }
    }

    /// <summary>
    /// The host's checked state. Used for broadcasting and restoring after forced options.
    /// </summary>
    public bool HostChecked { get; set; }

    /// <summary>
    /// The user's checked state. Persists across forced value changes.
    /// Used for saving/loading skirmish settings.
    /// </summary>
    public bool UserChecked { get; set; }

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public GameOptionCheckBox(GameSessionSetting setting)
    {
        Setting = setting;
        _isChecked = setting.Value != 0;
        HostChecked = _isChecked;
        UserChecked = _isChecked;
    }
}
