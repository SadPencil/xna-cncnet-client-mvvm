#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface ILANLobbyView : ISwitchableView
{
    ILANLobbyViewModel? ViewModel { get; set; }
}
