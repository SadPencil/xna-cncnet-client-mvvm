using CommunityToolkit.Mvvm.ComponentModel;
using DXMainClientViewModel.Domain.Multiplayer;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// Observable wrapper for a game option checkbox.
/// </summary>
public class GameOptionCheckBox : ObservableObject
{
    public IGameSessionSetting Setting { get; }

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

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public GameOptionCheckBox(IGameSessionSetting setting)
    {
        Setting = setting;
        _isChecked = setting.Value != 0;
    }
}
