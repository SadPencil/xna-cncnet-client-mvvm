using AvClientMvvmContract.Generic.OptionPanels;

namespace AvClientView.Generic.OptionPanels;

public interface IUpdaterOptionsPanelView
{
    IUpdaterOptionsPanelViewModel? ViewModel { get; set; }
}
