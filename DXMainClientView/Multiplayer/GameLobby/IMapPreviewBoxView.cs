#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer.GameLobby;

namespace DXMainClientView.Multiplayer.GameLobby;

public interface IMapPreviewBoxView
{
    IMapPreviewBoxViewModel? ViewModel { get; set; }
}
