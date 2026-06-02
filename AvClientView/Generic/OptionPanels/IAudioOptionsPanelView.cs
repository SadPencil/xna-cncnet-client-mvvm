using AvClientMvvmContract.Generic.OptionPanels;

namespace AvClientView.Generic.OptionPanels;

public interface IAudioOptionsPanelView
{
    IAudioOptionsPanelViewModel? ViewModel { get; set; }
}
