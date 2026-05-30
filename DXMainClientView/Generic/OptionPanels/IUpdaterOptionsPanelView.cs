using DXMainClientMvvmContract.Generic.OptionPanels;

namespace DXMainClientView.Generic.OptionPanels;

public interface IUpdaterOptionsPanelView
{
    IUpdaterOptionsPanelViewModel? ViewModel { get; set; }
}
