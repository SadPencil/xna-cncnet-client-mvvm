#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ICnCNetLobbyView : ISwitchableView
{
    ICnCNetLobbyViewModel? ViewModel { get; set; }
}
