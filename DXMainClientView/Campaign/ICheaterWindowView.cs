using DXMainClientViewModel.Campaign;

namespace DXMainClientView.Campaign;

public interface ICheaterWindowView
{
    ICheaterWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
