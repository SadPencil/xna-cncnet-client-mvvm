#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ICnCNetOptionsPanelView
{
    ICnCNetOptionsPanelViewModel? ViewModel { get; set; }
}
