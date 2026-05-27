#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IAudioOptionsPanelView
{
    IAudioOptionsPanelViewModel? ViewModel { get; set; }
}
