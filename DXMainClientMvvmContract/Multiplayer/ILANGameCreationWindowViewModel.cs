using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMVVMContract.Multiplayer;

public interface ILANGameCreationWindowViewModel : INotifyPropertyChanged
{
    string GameName { get; set; }
    bool IsLoadGameAvailable { get; }
    bool IsWindowVisible { get; set; }

    IRelayCommand CreateNewGameCommand { get; }
    IRelayCommand LoadGameCommand { get; }
    IRelayCommand CancelCommand { get; }
}
