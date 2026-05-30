using DXMainClientMVVMContract.Domain.Multiplayer;
using DXMainClientMVVMContract.Multiplayer.GameLobby;

using System.Collections.Generic;

using CommunityToolkit.Mvvm.ComponentModel;

using DXMainClientViewModel.Domain.Multiplayer;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// Observable wrapper for a game option dropdown.
/// </summary>
public class GameOptionDropDown : ObservableObject, IGameOptionDropDown
{
    public IGameSessionSetting Setting { get; }

    public string Name => Setting.Name;

    private int _selectedIndex;
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (SetProperty(ref _selectedIndex, value))
                Setting.Value = value;
        }
    }

    /// <summary>
    /// The host's selected index. Used for broadcasting and restoring after forced options.
    /// </summary>
    public int HostSelectedIndex { get; set; }

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    private IReadOnlyList<string> _items = System.Array.Empty<string>();
    public IReadOnlyList<string> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public GameOptionDropDown(IGameSessionSetting setting)
    {
        Setting = setting;
        _selectedIndex = setting.Value;
    }
}
