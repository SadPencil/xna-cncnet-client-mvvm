using DXMainClientMvvmContract.Generic.OptionPanels;

namespace DXMainClientView.Generic.OptionPanels;

public interface IGameOptionsPanelView
{
    IGameOptionsPanelViewModel? ViewModel { get; set; }
}
