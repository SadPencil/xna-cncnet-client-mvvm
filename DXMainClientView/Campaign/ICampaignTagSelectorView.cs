#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface ICampaignTagSelectorView
{
    event Action<string>? TagSelected;
    event Action? ShowAllRequested;
    event Action? CloseRequested;

    void Show();
    void Hide();
    void SetTagNames(IEnumerable<string> tagNames);
    void SetTagEnabled(string tagName, bool enabled);
    void SetShowAllEnabled(bool enabled);
    void SetTitle(string title);
}
