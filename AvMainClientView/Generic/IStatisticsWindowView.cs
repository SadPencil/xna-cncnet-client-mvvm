using AvMainClientMvvmContract.Generic;

namespace AvMainClientView.Generic;

public interface IStatisticsWindowView
{
    IStatisticsWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
