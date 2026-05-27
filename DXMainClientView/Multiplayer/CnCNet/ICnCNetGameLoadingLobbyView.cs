#nullable enable

namespace DXMainClientView;

public interface ICnCNetGameLoadingLobbyView : IGameLoadingLobbyView
{
    void SetBroadcastStatusText(string statusText);
    void SetFileHashStatusText(string statusText);
    void SetTunnelName(string tunnelName);
    void ShowTunnelSelectionWindow();
    void ShowCheaterWarning(string playerName);
    void SetTunnelButtonEnabled(bool enabled);
}
