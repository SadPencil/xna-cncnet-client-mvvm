#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IStatisticsWindowView
{
    IStatisticsWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
