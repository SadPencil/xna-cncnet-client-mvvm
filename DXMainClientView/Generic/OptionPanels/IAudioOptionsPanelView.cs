using DXMainClientMvvmContract.Generic.OptionPanels;

namespace DXMainClientView.Generic.OptionPanels;

public interface IAudioOptionsPanelView
{
    IAudioOptionsPanelViewModel? ViewModel { get; set; }
}
