#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IUpdaterOptionsPanelView
{
    IUpdaterOptionsPanelViewModel? ViewModel { get; set; }
}
