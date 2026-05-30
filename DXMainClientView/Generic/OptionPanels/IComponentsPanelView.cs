using DXMainClientMvvmContract.Generic.OptionPanels;

namespace DXMainClientView.Generic.OptionPanels;

public interface IComponentsPanelView
{
    IComponentsPanelViewModel? ViewModel { get; set; }
}
