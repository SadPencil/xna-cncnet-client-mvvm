using AvClientMvvmContract.Campaign;

namespace AvClientView.Campaign;

public interface ICheaterWindowView
{
    ICheaterWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
