#nullable enable

using System;
using System.Collections.Generic;

namespace DXMainClientView;

public interface ITunnelSelectionWindowView
{
    event Action? ApplyRequested;
    event Action? CancelRequested;
    event Action<string>? SelectedTunnelChanged;

    void Show();
    void Hide();
    void SetDescription(string description);
    void SetTunnels(IEnumerable<IReadOnlyList<string>> tunnels);
    void SetSelectedTunnelAddress(string tunnelAddress);
    void SetApplyEnabled(bool enabled);
}
