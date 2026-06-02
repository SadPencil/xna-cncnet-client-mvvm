using AvClientMvvmContract.Generic;

namespace AvClientView.Generic;

public interface ILoadingScreenView
{
    ILoadingScreenViewModel? ViewModel { get; set; }
}
