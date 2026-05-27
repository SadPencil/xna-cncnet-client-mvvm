using DXMainClientViewModel.Generic;

namespace DXMainClientView.Generic;

public interface IManualUpdateQueryWindowView
{
    IManualUpdateQueryWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
