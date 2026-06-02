using AvMainClientMvvmContract.Generic;

namespace AvMainClientView.Generic;

public interface IManualUpdateQueryWindowView
{
    IManualUpdateQueryWindowViewModel? ViewModel { get; set; }
}
