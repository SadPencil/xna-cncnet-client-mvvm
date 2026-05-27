#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Generic;

namespace DXMainClientView.Generic;

public interface IMainMenuView : ISwitchableView
{
    IMainMenuViewModel? ViewModel { get; set; }
}
