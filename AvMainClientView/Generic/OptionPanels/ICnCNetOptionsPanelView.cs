using AvMainClientMvvmContract.Generic.OptionPanels;

namespace AvMainClientView.Generic.OptionPanels;

public interface ICnCNetOptionsPanelView
{
    ICnCNetOptionsPanelViewModel? ViewModel { get; set; }
}
