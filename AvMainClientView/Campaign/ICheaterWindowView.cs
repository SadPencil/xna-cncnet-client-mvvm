using AvMainClientMvvmContract.Campaign;

namespace AvMainClientView.Campaign;

public interface ICheaterWindowView
{
    ICheaterWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
