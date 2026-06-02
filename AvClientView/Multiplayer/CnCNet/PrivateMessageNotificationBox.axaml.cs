using Avalonia.Controls;

using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

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
