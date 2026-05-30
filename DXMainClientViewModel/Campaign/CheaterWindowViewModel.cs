using DXMainClientMVVMContract.Campaign;

#nullable enable
using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Campaign;

public partial class CheaterWindowViewModel : ObservableObject, ICheaterWindowViewModel
{
    public CheaterWindowViewModel(Action onConfirm, Action onCancel)
    {
        ConfirmCommand = new RelayCommand(onConfirm);
        CancelCommand = new RelayCommand(onCancel);
    }

    [ObservableProperty]
    private string titleText = string.Empty;

    [ObservableProperty]
    private string messageText = string.Empty;

    public IRelayCommand ConfirmCommand { get; }
    public IRelayCommand CancelCommand { get; }
}


