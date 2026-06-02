using AvClientMvvmContract.Generic;

namespace AvClientView.Generic;

public interface IUpdateQueryWindowView
{
    IUpdateQueryWindowViewModel? ViewModel { get; set; }
}
