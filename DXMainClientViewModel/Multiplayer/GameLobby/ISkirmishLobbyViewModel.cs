using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

public interface ISkirmishLobbyViewModel : IGameLobbyViewModel
{
    bool ShowPlayerNamesInGame { get; set; }

    IRelayCommand AddAiPlayerCommand { get; }
    IRelayCommand RemoveSelectedPlayerCommand { get; }
    IRelayCommand RandomizeSidesCommand { get; }
}
