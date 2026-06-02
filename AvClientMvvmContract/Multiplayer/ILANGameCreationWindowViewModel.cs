using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer;

public interface ILANGameCreationWindowViewModel : INotifyPropertyChanged
{
    string GameName { get; set; }
    bool IsLoadGameAvailable { get; }
    bool IsVisible { get; set; }

    IRelayCommand CreateNewGameCommand { get; }
    IRelayCommand LoadGameCommand { get; }
    IRelayCommand CancelCommand { get; }
}
