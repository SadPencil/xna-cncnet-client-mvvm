#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IRecentPlayerTableView
{
    IRecentPlayerTableViewModel? ViewModel { get; set; }
}
