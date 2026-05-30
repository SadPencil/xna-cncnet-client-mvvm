using DXMainClientMvvmContract.Generic;

namespace DXMainClientView.Generic;

public interface IGameInProgressWindowView
{
    IGameInProgressWindowViewModel? ViewModel { get; set; }
}
