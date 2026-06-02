using AvMainClientMvvmContract.Generic;

namespace AvMainClientView.Generic;

public interface IGameInProgressWindowView
{
    IGameInProgressWindowViewModel? ViewModel { get; set; }
}
