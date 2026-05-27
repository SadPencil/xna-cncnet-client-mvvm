#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface ILANGameLobbyViewModel : IMultiplayerGameLobbyViewModel
{
    string LocalAddressText { get; }

    IRelayCommand BroadcastGameStateCommand { get; }
}
