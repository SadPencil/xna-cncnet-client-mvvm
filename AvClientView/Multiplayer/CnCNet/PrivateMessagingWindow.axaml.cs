using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Media;

using AvClientMvvmContract.Multiplayer.CnCNet;

using AvClientView.Services;

using Serilog;

namespace AvClientView.Multiplayer.CnCNet;

public partial class PrivateMessagingWindow : UserControl, IPrivateMessagingWindowView
{
    public AvClientView.Services.IIniLayoutOverlayService? IniOverlayService { get; set; }

    private const int MESSAGES_INDEX = 0;
    private const int FRIEND_LIST_VIEW_INDEX = 1;
    private const int ALL_PLAYERS_VIEW_INDEX = 2;
    private const int RECENT_PLAYERS_VIEW_INDEX = 3;

    public PrivateMessagingWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        // Wire tab button clicks to set SelectedTabIndex on ViewModel.
        // Matches old XNAClientTabControl.SelectedIndexChanged behavior.
        tabMessages.Click += (_, _) => { if (ViewModel != null) ViewModel.SelectedTabIndex = MESSAGES_INDEX; };
        tabFriendList.Click += (_, _) => { if (ViewModel != null) ViewModel.SelectedTabIndex = FRIEND_LIST_VIEW_INDEX; };
        tabAllPlayers.Click += (_, _) => { if (ViewModel != null) ViewModel.SelectedTabIndex = ALL_PLAYERS_VIEW_INDEX; };
        tabRecentPlayers.Click += (_, _) => { if (ViewModel != null) ViewModel.SelectedTabIndex = RECENT_PLAYERS_VIEW_INDEX; };
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var iniOverlay = IniOverlayService;
        iniOverlay?.ApplyLayout(this, "PrivateMessagingWindow");
    }

    public IPrivateMessagingWindowViewModel? ViewModel
    {
        get => DataContext as IPrivateMessagingWindowViewModel;
        set
        {
            if (DataContext is IPrivateMessagingWindowViewModel oldVm)
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;

            DataContext = value;

            if (value != null)
            {
                value.PropertyChanged += OnViewModelPropertyChanged;
                UpdateTabSelection(value.SelectedTabIndex);
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IPrivateMessagingWindowViewModel.SelectedTabIndex)
            && ViewModel != null)
        {
            UpdateTabSelection(ViewModel.SelectedTabIndex);
        }
    }

    /// <summary>
    /// Updates tab button visual state to indicate the selected tab.
    /// Selected tab is enabled (normal); unselected tabs are dimmed.
    /// </summary>
    private void UpdateTabSelection(int selectedIndex)
    {
        var buttons = new[] { tabMessages, tabFriendList, tabAllPlayers, tabRecentPlayers };
        for (int i = 0; i < buttons.Length; i++)
        {
            if (i == selectedIndex)
            {
                buttons[i].IsEnabled = true;
                buttons[i].Opacity = 1.0;
            }
            else
            {
                buttons[i].IsEnabled = true;
                buttons[i].Opacity = 0.5;
            }
        }

        Log.Debug($"[PrivateMessagingWindow] Tab selected: {selectedIndex}");
    }

    void ISwitchableView.Show() => IsVisible = true;
    void ISwitchableView.Hide() => IsVisible = false;
    string ISwitchableView.GetDisplayName() => "Private Messages";
}
