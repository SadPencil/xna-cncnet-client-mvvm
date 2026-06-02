using AvMainClientMvvmContract.Generic.OptionPanels;

namespace AvMainClientView.Generic.OptionPanels;

public interface IAudioOptionsPanelView
{
    IAudioOptionsPanelViewModel? ViewModel { get; set; }
}
