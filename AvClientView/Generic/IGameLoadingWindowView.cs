using AvClientMvvmContract.Generic;

namespace AvClientView.Generic;

public interface IGameLoadingWindowView
{
    IGameLoadingWindowViewModel? ViewModel { get; set; }
}
