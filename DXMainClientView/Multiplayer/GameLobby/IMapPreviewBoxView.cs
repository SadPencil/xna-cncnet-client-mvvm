#nullable enable

using System;

namespace DXMainClientView;

public interface IMapPreviewBoxView
{
    event Action<int>? StartingLocationSelected;
    event Action? FavoriteToggled;

    void SetMapPreviewPath(string previewPath);
    void SetMapName(string mapName);
    void SetMapDimensionsText(string dimensionsText);
    void SetCoopBriefing(string briefingText);
    void SetStartingLocationCount(int count);
    void SetStartingLocationLabel(int index, string labelText);
    void SetStartingLocationEnabled(int index, bool enabled);
    void SetSelectedStartingLocation(int index);
    void SetContextMenuEnabled(bool enabled);
    void SetStartLocationSelectionEnabled(bool enabled);
    void SetFavorite(bool favorite);
    void SetExtraTexturesVisible(bool visible);
}
