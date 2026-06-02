using Avalonia.Controls;

using AvClientMvvmContract.Generic;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView.Generic;

public partial class PrivacyNotification : UserControl, IPrivacyNotificationView
{
    public PrivacyNotification()
    {
        InitializeComponent();
        ViewModel = ViewConstants.ServiceProvider.GetRequiredService<IPrivacyNotificationViewModel>();
    }

    public IPrivacyNotificationViewModel? ViewModel
    {
        get => DataContext as IPrivacyNotificationViewModel;
        set => DataContext = value;
    }
}
