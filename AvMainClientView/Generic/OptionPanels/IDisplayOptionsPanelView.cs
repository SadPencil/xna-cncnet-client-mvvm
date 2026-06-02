using AvMainClientMvvmContract.Generic.OptionPanels;

namespace AvMainClientView.Generic.OptionPanels;

public interface IDisplayOptionsPanelView
{
    IDisplayOptionsPanelViewModel? ViewModel { get; set; }
}
