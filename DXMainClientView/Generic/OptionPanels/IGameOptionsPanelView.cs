#nullable enable

namespace DXMainClientView;

public interface IGameOptionsPanelView
{
    void SetPlayerName(string playerName);
    void SetScrollRate(int value);
    void SetScrollCoasting(bool enabled);
    void SetTargetLines(bool enabled);
    void SetTooltips(bool enabled);
    void SetShowHiddenObjects(bool enabled);
    void SetBlackChatBackground(bool enabled);
    void SetAltToUndeploy(bool enabled);
    void ShowHotkeyConfiguration();
}
