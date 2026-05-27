#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IDisplayOptionsPanelView
{
    IDisplayOptionsPanelViewModel? ViewModel { get; set; }
}
