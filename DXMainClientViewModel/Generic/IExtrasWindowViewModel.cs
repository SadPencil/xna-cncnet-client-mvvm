using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic;

public interface IExtrasWindowViewModel : INotifyPropertyChanged
{
    bool IsStatisticsAvailable { get; }
    bool IsMapEditorAvailable { get; }

    IRelayCommand OpenStatisticsCommand { get; }
    IRelayCommand OpenMapEditorCommand { get; }
    IRelayCommand OpenCreditsCommand { get; }
    IRelayCommand CloseCommand { get; }
}
