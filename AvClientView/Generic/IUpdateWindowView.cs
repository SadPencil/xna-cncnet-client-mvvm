using AvClientMvvmContract.Generic;

namespace AvClientView.Generic;

public interface IUpdateWindowView
{
    IUpdateWindowViewModel? ViewModel { get; set; }
}
