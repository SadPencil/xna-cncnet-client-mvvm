#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ILANLobbyView : ISwitchableView
{
    ILANLobbyViewModel? ViewModel { get; set; }
}
