using AvMainClientMvvmContract.Generic;

namespace AvMainClientView.Generic;

public interface IMainMenuView : ISwitchableView
{
    IMainMenuViewModel? ViewModel { get; set; }
}
