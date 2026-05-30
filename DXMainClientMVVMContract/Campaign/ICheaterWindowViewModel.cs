using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMVVMContract.Campaign;

public interface ICheaterWindowViewModel : INotifyPropertyChanged
{
    string TitleText { get; }
    string MessageText { get; }

    IRelayCommand ConfirmCommand { get; }
    IRelayCommand CancelCommand { get; }
}
