using AvClientMvvmContract.Generic.OptionPanels;

namespace AvClientView.Generic.OptionPanels;

public interface ICnCNetOptionsPanelView
{
    ICnCNetOptionsPanelViewModel? ViewModel { get; set; }
}
