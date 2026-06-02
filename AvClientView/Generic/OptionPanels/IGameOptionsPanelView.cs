using AvClientMvvmContract.Generic.OptionPanels;

namespace AvClientView.Generic.OptionPanels;

public interface IGameOptionsPanelView
{
    IGameOptionsPanelViewModel? ViewModel { get; set; }
}
