using AvClientMvvmContract.Generic;

namespace AvClientView.Generic;

public interface IStatisticsWindowView
{
    IStatisticsWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
