#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ISkirmishLobbyView : IGameLobbyView
{
    new ISkirmishLobbyViewModel? ViewModel { get; set; }
}
