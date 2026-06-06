using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Generic;

public interface IExtrasWindowViewModel : INotifyPropertyChanged
{
    bool IsVisible { get; set; }
    bool IsMapEditorAllowed { get; }

    IRelayCommand OpenStatisticsCommand { get; }
    IRelayCommand OpenMapEditorCommand { get; }
    IRelayCommand OpenCreditsCommand { get; }
    IRelayCommand CloseCommand { get; }
}
