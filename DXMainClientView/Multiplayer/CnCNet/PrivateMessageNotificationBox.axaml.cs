using DXMainClientMVVMContract.Multiplayer.CnCNet;

using Avalonia.Controls;

namespace DXMainClientView.Multiplayer.CnCNet;

public partial class PrivateMessageNotificationBox : UserControl, IPrivateMessageNotificationBoxView
{
    public PrivateMessageNotificationBox()
    {
        InitializeComponent();
    }

    public IPrivateMessageNotificationBoxViewModel? ViewModel
    {
        get => DataContext as IPrivateMessageNotificationBoxViewModel;
        set => DataContext = value;
    }
}
