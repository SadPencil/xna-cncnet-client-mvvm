#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Generic.OptionPanels;

namespace DXMainClientView.Generic.OptionPanels;

public interface IDisplayOptionsPanelView
{
    IDisplayOptionsPanelViewModel? ViewModel { get; set; }
}
