using DXMainClientMvvmContract.Generic.OptionPanels;

namespace DXMainClientView.Generic.OptionPanels;

public interface IDisplayOptionsPanelView
{
    IDisplayOptionsPanelViewModel? ViewModel { get; set; }
}
