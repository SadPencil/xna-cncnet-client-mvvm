#nullable enable
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface ILANGameCreationWindowViewModel : INotifyPropertyChanged
{
    string GameName { get; set; }
    bool IsLoadGameAvailable { get; }

    IRelayCommand CreateNewGameCommand { get; }
    IRelayCommand LoadGameCommand { get; }
    IRelayCommand CancelCommand { get; }
}
