using DXMainClientMvvmContract.Generic.OptionPanels;

namespace DXMainClientView.Generic.OptionPanels;

public interface ICnCNetOptionsPanelView
{
    ICnCNetOptionsPanelViewModel? ViewModel { get; set; }
}
