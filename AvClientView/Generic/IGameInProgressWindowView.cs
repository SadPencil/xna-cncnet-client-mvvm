using AvClientMvvmContract.Generic;

namespace AvClientView.Generic;

public interface IGameInProgressWindowView
{
    IGameInProgressWindowViewModel? ViewModel { get; set; }
}
