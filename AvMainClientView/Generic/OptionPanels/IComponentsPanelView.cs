using AvMainClientMvvmContract.Generic.OptionPanels;

namespace AvMainClientView.Generic.OptionPanels;

public interface IComponentsPanelView
{
    IComponentsPanelViewModel? ViewModel { get; set; }
}
