using AvMainClientMvvmContract.Generic.OptionPanels;

namespace AvMainClientView.Generic.OptionPanels;

public interface IGameOptionsPanelView
{
    IGameOptionsPanelViewModel? ViewModel { get; set; }
}
