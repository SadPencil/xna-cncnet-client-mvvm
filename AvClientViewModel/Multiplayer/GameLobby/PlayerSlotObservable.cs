using AvClientMvvmContract.Multiplayer.GameLobby;

using CommunityToolkit.Mvvm.ComponentModel;

namespace AvClientViewModel.Multiplayer.GameLobby;

public partial class PlayerSlotObservable : ObservableObject, IPlayerSlotObservable
{
    [ObservableProperty]
    public partial string PlayerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IPlayerSlotDropdown<IPlayerName> Name { get; set; } = new PlayerSlotDropdown<IPlayerName>();

    [ObservableProperty]
    public partial IPlayerSlotDropdown<IPlayerSide> Side { get; set; } = new PlayerSlotDropdown<IPlayerSide>();

    [ObservableProperty]
    public partial IPlayerSlotDropdown<IPlayerColor> Color { get; set; } = new PlayerSlotDropdown<IPlayerColor>();

    [ObservableProperty]
    public partial IPlayerSlotDropdown<IPlayerStart> Start { get; set; } = new PlayerSlotDropdown<IPlayerStart>();

    [ObservableProperty]
    public partial IPlayerSlotDropdown<IPlayerTeam> Team { get; set; } = new PlayerSlotDropdown<IPlayerTeam>();
}
