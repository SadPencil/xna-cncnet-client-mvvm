using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvMainClientMvvmContract.Generic;

public interface IManualUpdateQueryWindowViewModel : INotifyPropertyChanged
{
    string DescriptionText { get; }
    bool IsVisible { get; set; }

    IRelayCommand ViewDownloadsCommand { get; }
    IRelayCommand CloseCommand { get; }
}
