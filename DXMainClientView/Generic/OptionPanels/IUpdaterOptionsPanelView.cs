#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface IUpdaterOptionsPanelView
{
    event Action? ForceUpdateRequested;

    void SetCheckForUpdates(bool enabled);
    void SetUpdateServerOptions(IEnumerable<string> updateServers);
    void SetSelectedUpdateServerIndex(int selectedIndex);
    void SetMoveUpEnabled(bool enabled);
    void SetMoveDownEnabled(bool enabled);
    void SetForceUpdateEnabled(bool enabled);
    void RefreshPanel();
}
