using AvClientMvvmContract.Generic.OptionPanels;

namespace AvClientView.Generic.OptionPanels;

public interface IDisplayOptionsPanelView
{
    IDisplayOptionsPanelViewModel? ViewModel { get; set; }
}
