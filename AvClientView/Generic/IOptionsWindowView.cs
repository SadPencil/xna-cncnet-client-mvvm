using AvClientMvvmContract.Generic;

namespace AvClientView.Generic;

public interface IOptionsWindowView
{
    IOptionsWindowViewModel? ViewModel { get; set; }
}
