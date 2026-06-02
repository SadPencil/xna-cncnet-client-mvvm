using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvMainClientMvvmContract.Generic;

public interface IUpdateWindowViewModel : INotifyPropertyChanged
{
    string DescriptionText { get; }
    string CurrentFileName { get; }
    int CurrentFilePercentage { get; }
    int TotalPercentage { get; }
    string UpdaterStatusText { get; }
    bool IsVisible { get; set; }

    IRelayCommand CancelCommand { get; }
}
