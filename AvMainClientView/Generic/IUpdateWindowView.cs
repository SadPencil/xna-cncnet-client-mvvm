using AvMainClientMvvmContract.Generic;

namespace AvMainClientView.Generic;

public interface IUpdateWindowView
{
    IUpdateWindowViewModel? ViewModel { get; set; }
}
