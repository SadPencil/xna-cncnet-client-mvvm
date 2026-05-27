using DXMainClientViewModel.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface ILANLobbyView : ISwitchableView
{
    ILANLobbyViewModel? ViewModel { get; set; }
}
