using AvMainClientMvvmContract.Generic;

namespace AvMainClientView.Generic;

public interface IOptionsWindowView
{
    IOptionsWindowViewModel? ViewModel { get; set; }
}
