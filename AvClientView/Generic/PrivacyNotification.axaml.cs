using Avalonia.Controls;

using AvClientMvvmContract.Generic;

namespace AvClientView.Generic;

public partial class PrivacyNotification : UserControl, IPrivacyNotificationView
{
    public PrivacyNotification()
    {
        InitializeComponent();
        // Break DataContext inheritance from parent so compiled bindings
        // don't evaluate against the parent's ViewModel before our
        // ViewModel is assigned via SetPrivacyNotificationViewModel.
        DataContext = null;
    }

    public IPrivacyNotificationViewModel? ViewModel
    {
        get => DataContext as IPrivacyNotificationViewModel;
        set => DataContext = value;
    }
}
