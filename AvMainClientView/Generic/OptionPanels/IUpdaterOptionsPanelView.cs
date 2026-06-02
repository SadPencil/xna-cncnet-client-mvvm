using AvMainClientMvvmContract.Generic.OptionPanels;

namespace AvMainClientView.Generic.OptionPanels;

public interface IUpdaterOptionsPanelView
{
    IUpdaterOptionsPanelViewModel? ViewModel { get; set; }
}
