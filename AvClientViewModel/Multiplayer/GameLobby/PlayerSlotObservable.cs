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

    private IPlayerSlotDropdown<IPlayerName> _name = new PlayerSlotDropdown<IPlayerName>();
    public IPlayerSlotDropdown<IPlayerName> Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private IPlayerSlotDropdown<IPlayerSide> _side = new PlayerSlotDropdown<IPlayerSide>();
    public IPlayerSlotDropdown<IPlayerSide> Side
    {
        get => _side;
        set => SetProperty(ref _side, value);
    }

    private IPlayerSlotDropdown<IPlayerColor> _color = new PlayerSlotDropdown<IPlayerColor>();
    public IPlayerSlotDropdown<IPlayerColor> Color
    {
        get => _color;
        set => SetProperty(ref _color, value);
    }

    private IPlayerSlotDropdown<IPlayerStart> _start = new PlayerSlotDropdown<IPlayerStart>();
    public IPlayerSlotDropdown<IPlayerStart> Start
    {
        get => _start;
        set => SetProperty(ref _start, value);
    }

    private IPlayerSlotDropdown<IPlayerTeam> _team = new PlayerSlotDropdown<IPlayerTeam>();
    public IPlayerSlotDropdown<IPlayerTeam> Team
    {
        get => _team;
        set => SetProperty(ref _team, value);
    }
}
