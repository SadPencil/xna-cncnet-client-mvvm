using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Multiplayer.GameLobby;

public interface ISkirmishLobbyViewModel : IGameLobbyViewModel
{
    bool IsVisible { get; set; }
    bool ShowPlayerNamesInGame { get; set; }

    // Error/Notice display (View binds to these)
    bool IsErrorVisible { get; }
    string ErrorMessage { get; }
    bool IsNoticeVisible { get; }
    string NoticeMessage { get; }

    IRelayCommand AddAiPlayerCommand { get; }
    IRelayCommand RemoveSelectedPlayerCommand { get; }
    IRelayCommand RandomizeSidesCommand { get; }
    IRelayCommand DismissErrorCommand { get; }
    IRelayCommand DismissNoticeCommand { get; }
}
