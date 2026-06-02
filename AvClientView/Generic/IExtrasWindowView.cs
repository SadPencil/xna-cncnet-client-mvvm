using AvClientMvvmContract.Generic;

namespace AvClientView.Generic;

public interface IExtrasWindowView
{
    IExtrasWindowViewModel? ViewModel { get; set; }
}
