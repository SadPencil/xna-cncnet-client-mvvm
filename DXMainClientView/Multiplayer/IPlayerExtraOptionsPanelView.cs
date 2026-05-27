#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IPlayerExtraOptionsPanelView
{
    IPlayerExtraOptionsPanelViewModel? ViewModel { get; set; }
}
