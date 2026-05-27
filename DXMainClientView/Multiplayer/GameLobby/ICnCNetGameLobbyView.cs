#nullable enable

namespace DXMainClientView;

public interface ICnCNetGameLobbyView : IMultiplayerGameLobbyView
{
    void SetTunnelName(string tunnelName);
    void SetTunnelPingText(string pingText);
    void SetTunnelButtonEnabled(bool enabled);
    void ShowTunnelSelectionWindow();
    void ShowMapSharingConfirmation();
    void ShowPresetWindow();
}
