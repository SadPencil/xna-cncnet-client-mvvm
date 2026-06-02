using AvMainClientMvvmContract.Generic;

namespace AvMainClientView.Generic;

public interface IGameLoadingWindowView
{
    IGameLoadingWindowViewModel? ViewModel { get; set; }
}
