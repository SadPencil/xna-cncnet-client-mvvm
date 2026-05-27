#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IManualUpdateQueryWindowView
{
    IManualUpdateQueryWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
