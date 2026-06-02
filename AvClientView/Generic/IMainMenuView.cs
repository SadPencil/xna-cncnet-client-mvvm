using AvClientMvvmContract.Generic;

namespace AvClientView.Generic;

public interface IMainMenuView : ISwitchableView
{
    IMainMenuViewModel? ViewModel { get; set; }
}
