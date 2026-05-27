#nullable enable

using System.Collections.Generic;

namespace DXMainClientView;

public interface IGameInformationPanelView
{
    void Show();
    void Hide();
    void SetHeaderText(string headerText);
    void SetMapName(string mapName);
    void SetHostName(string hostName);
    void SetPlayerCountText(string playerCountText);
    void SetPingText(string pingText);
    void SetGameVersionText(string versionText);
    void SetMapPreviewPath(string previewPath);
    void SetOptionValues(IReadOnlyDictionary<string, string> optionValues);
    void Clear();
}
