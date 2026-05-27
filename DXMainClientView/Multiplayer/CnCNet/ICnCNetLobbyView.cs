#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface ICnCNetLobbyView : ISwitchableView
{
    ICnCNetLobbyViewModel? ViewModel { get; set; }
}
