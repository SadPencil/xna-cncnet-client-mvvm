using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using AvClientMvvmContract.Multiplayer.CnCNet;

using AvClientView.Services;


namespace AvClientView.Multiplayer.CnCNet;

public partial class PrivateMessagingWindow : UserControl, IPrivateMessagingWindowView
{
    private IIniLayoutOverlayService? _iniOverlayService;
    public AvClientView.Services.IIniLayoutOverlayService? IniOverlayService
    {
        get => _iniOverlayService;
        set
        {
            _iniOverlayService = value;
            // IniOverlayService is set by MainMenu AFTER OnLoaded has already fired,
            // so we must apply the layout here when it arrives.
            if (value != null)
                ApplyLayoutAndCaptureBrushes(value);
        }
    }

    private const int MESSAGES_INDEX = 0;
    private const int FRIEND_LIST_VIEW_INDEX = 1;
    private const int ALL_PLAYERS_VIEW_INDEX = 2;
    private const int RECENT_PLAYERS_VIEW_INDEX = 3;

    private IImageBrush? _tabIdleBrush;
    private IImageBrush? _tabHoverBrush;
    private Button[]? _tabButtons;
    private int _selectedTabIndex;
    private bool _layoutApplied;

    public PrivateMessagingWindow()
    {
        InitializeComponent();

        // Wire tab button clicks to set SelectedTabIndex on ViewModel.
        // Matches old XNAClientTabControl.SelectedIndexChanged behavior.
        tabMessages.Click += (_, _) => { if (ViewModel != null) ViewModel.SelectedTabIndex = MESSAGES_INDEX; };
        tabFriendList.Click += (_, _) => { if (ViewModel != null) ViewModel.SelectedTabIndex = FRIEND_LIST_VIEW_INDEX; };
        tabAllPlayers.Click += (_, _) => { if (ViewModel != null) ViewModel.SelectedTabIndex = ALL_PLAYERS_VIEW_INDEX; };
        tabRecentPlayers.Click += (_, _) => { if (ViewModel != null) ViewModel.SelectedTabIndex = RECENT_PLAYERS_VIEW_INDEX; };
    }

    private void ApplyLayoutAndCaptureBrushes(IIniLayoutOverlayService iniOverlay)
    {
        if (_layoutApplied)
            return;
        _layoutApplied = true;

        iniOverlay.ApplyLayout(this, "PrivateMessagingWindow");

        _tabButtons = new[] { tabMessages, tabFriendList, tabAllPlayers, tabRecentPlayers };

        // Capture the idle brush from the first tab button (all share the same texture).
        _tabIdleBrush = tabMessages.Background as IImageBrush;

        // Load hover texture using the `_c` convention (e.g. 133pxbtn_c.png).
        string? hoverPath = iniOverlay.FindTextureFile("133pxbtn_c.png");
        if (hoverPath != null)
        {
            _tabHoverBrush = new ImageBrush(new Bitmap(hoverPath))
            {
                Stretch = Stretch.Fill,
                TileMode = TileMode.None
            };
        }

        // The INI overlay's ApplyStandardButtonTextures installs PointerEntered/Exited
        // handlers that swap idle/hover brushes. Those handlers fire BEFORE ours
        // (we subscribe later). We intercept PointerExited to re-apply the correct
        // background for the selected tab.
        foreach (var btn in _tabButtons)
        {
            btn.PointerExited += (_, _) =>
            {
                if (_tabButtons == null || _tabIdleBrush == null)
                    return;

                int idx = System.Array.IndexOf(_tabButtons, btn);
                btn.Background = (idx == _selectedTabIndex && _tabHoverBrush != null)
                    ? _tabHoverBrush
                    : _tabIdleBrush;
            };
        }

        // Apply initial selection visual.
        if (ViewModel != null)
            UpdateTabHover(ViewModel.SelectedTabIndex);
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
                UpdateTabHover(value.SelectedTabIndex);
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IPrivateMessagingWindowViewModel.SelectedTabIndex)
            && ViewModel != null)
        {
            UpdateTabHover(ViewModel.SelectedTabIndex);
        }
    }

    /// <summary>
    /// Shows the selected tab button with its hover texture and others with idle.
    /// Matches old XNAClientTabControl behavior where the active tab is "always hovered".
    /// </summary>
    private void UpdateTabHover(int selectedIndex)
    {
        _selectedTabIndex = selectedIndex;

        if (_tabButtons == null || _tabIdleBrush == null)
            return;

        for (int i = 0; i < _tabButtons.Length; i++)
        {
            _tabButtons[i].Background = (i == selectedIndex && _tabHoverBrush != null)
                ? _tabHoverBrush
                : _tabIdleBrush;
        }
    }

    void ISwitchableView.Show() => IsVisible = true;
    void ISwitchableView.Hide() => IsVisible = false;
    string ISwitchableView.GetDisplayName() => "Private Messages";
}
