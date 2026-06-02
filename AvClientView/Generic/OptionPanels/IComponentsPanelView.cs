using AvClientMvvmContract.Generic.OptionPanels;

namespace AvClientView.Generic.OptionPanels;

public interface IComponentsPanelView
{
    IComponentsPanelViewModel? ViewModel { get; set; }
}
