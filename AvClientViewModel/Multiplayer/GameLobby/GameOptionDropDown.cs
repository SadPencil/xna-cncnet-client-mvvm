using System.Collections.Generic;

using AvClientMvvmContract.Domain.Multiplayer;
using AvClientMvvmContract.Multiplayer.GameLobby;

using AvClientViewModel.Domain.Multiplayer;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// Observable wrapper for a game option dropdown.
/// </summary>
public partial class GameOptionDropDown : ObservableObject, IGameOptionDropDown
{
    public GameSessionSetting Setting { get; }

    public string Name => Setting.Name;

    public string DisplayName => !string.IsNullOrEmpty(Setting.OptionName) ? Setting.OptionName : Setting.Name;

    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    partial void OnSelectedIndexChanged(int value)
    {
        Setting.Value = value;
    }

    /// <summary>
    /// The host's selected index. Used for broadcasting and restoring after forced options.
    /// </summary>
    public int HostSelectedIndex { get; set; }

    /// <summary>
    /// The user's selected index. Persists across forced value changes.
    /// Used for saving/loading skirmish settings.
    /// </summary>
    public int UserSelectedIndex { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial IReadOnlyList<string> Items { get; set; } = System.Array.Empty<string>();

    public GameOptionDropDown(GameSessionSetting setting)
    {
        Setting = setting;
        SelectedIndex = setting.Value;
        UserSelectedIndex = setting.Value;
    }
}
