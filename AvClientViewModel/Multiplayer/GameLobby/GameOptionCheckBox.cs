using AvClientMvvmContract.Domain.Multiplayer;
using AvClientMvvmContract.Multiplayer.GameLobby;

using AvClientViewModel.Domain.Multiplayer;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// Observable wrapper for a game option checkbox.
/// </summary>
public partial class GameOptionCheckBox : ObservableObject, IGameOptionCheckBox
{
    public GameSessionSetting Setting { get; }

    public string Name => Setting.Name;

    public string DisplayName => !string.IsNullOrEmpty(Setting.Text) ? Setting.Text : Setting.Name;

    [ObservableProperty]
    public partial bool IsChecked { get; set; }

    partial void OnIsCheckedChanged(bool value)
    {
        Setting.Value = value ? 1 : 0;
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

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    public GameOptionCheckBox(GameSessionSetting setting)
    {
        Setting = setting;
        IsChecked = setting.Value != 0;
        HostChecked = IsChecked;
        UserChecked = IsChecked;
    }
}
