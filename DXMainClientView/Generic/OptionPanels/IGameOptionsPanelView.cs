#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Generic.OptionPanels;

namespace DXMainClientView.Generic.OptionPanels;

public interface IGameOptionsPanelView
{
    IGameOptionsPanelViewModel? ViewModel { get; set; }
}
