using AvClientMvvmContract.Multiplayer.CnCNet;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using AvClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientView.Multiplayer.CnCNet;

public partial class PrivateMessagingWindow : UserControl, IPrivateMessagingWindowView
{
    public PrivateMessagingWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SetupMessageInputEnterKey();
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "PrivateMessagingWindow");
    }

    private void SetupMessageInputEnterKey()
    {
        if (tbMessageInput != null)
        {
            tbMessageInput.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Return && ViewModel?.SendMessageCommand.CanExecute(null) == true)
                {
                    ViewModel.SendMessageCommand.Execute(null);
                    e.Handled = true;
                }
            };
        }
    }

    public IPrivateMessagingWindowViewModel? ViewModel
    {
        get => DataContext as IPrivateMessagingWindowViewModel;
        set => DataContext = value;
    }

    void ISwitchableView.Show() => IsVisible = true;
    void ISwitchableView.Hide() => IsVisible = false;
    string ISwitchableView.GetDisplayName() => "Private Messages";
}
