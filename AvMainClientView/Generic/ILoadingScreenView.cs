using AvMainClientMvvmContract.Generic;

namespace AvMainClientView.Generic;

public interface ILoadingScreenView
{
    ILoadingScreenViewModel? ViewModel { get; set; }
}
