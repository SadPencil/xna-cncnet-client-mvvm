using AvClientMvvmContract.Multiplayer.GameLobby;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Multiplayer.GameLobby;

public class PlayerSlotObservable : ObservableObject, IPlayerSlotObservable
{
    private string _playerName = string.Empty;
    public string PlayerName
    {
        get => _playerName;
        set => SetProperty(ref _playerName, value);
    }

    private IPlayerSlotDropdown _name = new PlayerSlotDropdown();
    public IPlayerSlotDropdown Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private IPlayerSlotDropdown _side = new PlayerSlotDropdown();
    public IPlayerSlotDropdown Side
    {
        get => _side;
        set => SetProperty(ref _side, value);
    }

    private IPlayerSlotDropdown _color = new PlayerSlotDropdown();
    public IPlayerSlotDropdown Color
    {
        get => _color;
        set => SetProperty(ref _color, value);
    }

    private IPlayerSlotDropdown _start = new PlayerSlotDropdown();
    public IPlayerSlotDropdown Start
    {
        get => _start;
        set => SetProperty(ref _start, value);
    }

    private IPlayerSlotDropdown _team = new PlayerSlotDropdown();
    public IPlayerSlotDropdown Team
    {
        get => _team;
        set => SetProperty(ref _team, value);
    }
}
