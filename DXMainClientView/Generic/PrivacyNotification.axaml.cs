using Avalonia.Controls;
using DXMainClientViewModel.Generic;

namespace DXMainClientView.Generic;

public partial class PrivacyNotification : UserControl, IPrivacyNotificationView
{
    public PrivacyNotification()
    {
        InitializeComponent();
    }

    public IPrivacyNotificationViewModel? ViewModel
    {
        get => DataContext as IPrivacyNotificationViewModel;
        set => DataContext = value;
    }
}
