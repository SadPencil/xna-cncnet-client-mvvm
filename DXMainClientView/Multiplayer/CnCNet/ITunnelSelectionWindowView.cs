#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ITunnelSelectionWindowView
{
    ITunnelSelectionWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
