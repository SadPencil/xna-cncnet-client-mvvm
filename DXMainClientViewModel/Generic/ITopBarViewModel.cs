#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface ITopBarViewModel
{
    string CurrentTimeText { get; }
    string ConnectionStatusText { get; }
    string PlayerCountText { get; }
    bool IsCnCNetButtonVisible { get; }
    bool IsPrivateMessagesButtonVisible { get; }

    IRelayCommand OpenMainMenuCommand { get; }
    IRelayCommand OpenCnCNetLobbyCommand { get; }
    IRelayCommand OpenPrivateMessagesCommand { get; }
    IRelayCommand OpenOptionsCommand { get; }
    IRelayCommand LogoutCommand { get; }

    void Initialize();
}
