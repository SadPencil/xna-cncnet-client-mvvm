using AvMainClientMvvmContract.Generic;

using Avalonia.Controls;

using Microsoft.Extensions.DependencyInjection;

namespace AvMainClientView.Generic;

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
