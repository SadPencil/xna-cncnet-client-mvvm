#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IComponentsPanelView
{
    IComponentsPanelViewModel? ViewModel { get; set; }
}
