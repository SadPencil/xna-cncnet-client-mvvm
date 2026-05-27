#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IMainMenuView : ISwitchableView
{
    IMainMenuViewModel? ViewModel { get; set; }
}
