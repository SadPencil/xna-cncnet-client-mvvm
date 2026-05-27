using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic;

public interface IUpdateWindowViewModel : INotifyPropertyChanged
{
    string NewVersionText { get; }
    string StatusText { get; }
    string CurrentFileText { get; }
    int CurrentFileProgressPercentage { get; }
    int TotalProgressPercentage { get; }

    IAsyncRelayCommand StartUpdateCommand { get; }
    IRelayCommand CancelUpdateCommand { get; }
}
