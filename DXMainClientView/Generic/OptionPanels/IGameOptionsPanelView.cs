#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IGameOptionsPanelView
{
    IGameOptionsPanelViewModel? ViewModel { get; set; }
}
