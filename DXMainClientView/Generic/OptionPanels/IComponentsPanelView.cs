#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Generic.OptionPanels;

namespace DXMainClientView.Generic.OptionPanels;

public interface IComponentsPanelView
{
    IComponentsPanelViewModel? ViewModel { get; set; }
}
