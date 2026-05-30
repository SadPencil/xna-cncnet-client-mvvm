using DXMainClientMvvmContract.Generic;

namespace DXMainClientView.Generic;

public interface IMainMenuView : ISwitchableView
{
    IMainMenuViewModel? ViewModel { get; set; }
}
