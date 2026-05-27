#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IUpdateWindowViewModel
{
    string NewVersionText { get; }
    string StatusText { get; }
    string CurrentFileText { get; }
    int CurrentFileProgressPercentage { get; }
    int TotalProgressPercentage { get; }

    IAsyncRelayCommand StartUpdateCommand { get; }
    IRelayCommand CancelUpdateCommand { get; }
}
