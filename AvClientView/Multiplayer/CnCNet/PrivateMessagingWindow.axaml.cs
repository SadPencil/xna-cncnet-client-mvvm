using System;
using System.ComponentModel;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.Online;

using AvClientView.Services;

using ClientCore.Extensions;


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

        SetupUserListHandlers();
        SetupMessagesListHandlers();
        SetupRecentPlayersContextMenu();
    }

    private void SetupUserListHandlers()
    {
        // Double-click on user → switch to Messages tab
        userListBox.DoubleTapped += (_, _) =>
        {
            if (ViewModel != null)
                ViewModel.SelectedTabIndex = MESSAGES_INDEX;
        };

        // Right-click on user → context menu
        userListBox.ContextRequested += (s, e) =>
        {
            e.Handled = true;
            ShowUserContextMenu(e);
        };
    }

    private void ShowUserContextMenu(ContextRequestedEventArgs e)
    {
        if (ViewModel == null)
            return;

        SelectListBoxItemFromEvent(userListBox, e);

        if (userListBox.SelectedIndex < 0 || userListBox.SelectedIndex >= ViewModel.UserNames.Count)
            return;

        var contextMenu = new ContextMenu();
        PopulateUserContextMenu(contextMenu);
        contextMenu.Open(userListBox);
    }

    private void PopulateUserContextMenu(ContextMenu menu)
    {
        if (ViewModel == null)
            return;

        menu.Items.Clear();

        var pmItem = new MenuItem { Header = "Private Message" };
        pmItem.Click += (_, _) => ViewModel.SelectedTabIndex = MESSAGES_INDEX;
        menu.Items.Add(pmItem);

        menu.Items.Add(new Separator());

        var friendItem = new MenuItem { Header = "Toggle Friend" };
        friendItem.Click += (_, _) => ViewModel.ToggleSelectedUserFriendCommand.Execute(null);
        menu.Items.Add(friendItem);

        var blockItem = new MenuItem { Header = "Toggle Block" };
        blockItem.Click += (_, _) => ViewModel.ToggleSelectedUserIgnoreCommand.Execute(null);
        menu.Items.Add(blockItem);

        menu.Items.Add(new Separator());

        var inviteItem = new MenuItem { Header = "Invite to Game" };
        inviteItem.Click += (_, _) => ViewModel.InviteSelectedUserToGameCommand.Execute(null);
        menu.Items.Add(inviteItem);

        var joinItem = new MenuItem { Header = "Join Game" };
        joinItem.Click += (_, _) => ViewModel.JoinSelectedUserGameCommand.Execute(null);
        menu.Items.Add(joinItem);
    }

    private static void SelectListBoxItemFromEvent(ListBox listBox, ContextRequestedEventArgs e)
    {
        if (e.Source is Control control)
        {
            var listBoxItem = control.FindAncestorOfType<ListBoxItem>();
            if (listBoxItem != null)
                listBox.SelectedIndex = listBox.IndexFromContainer(listBoxItem);
        }
    }

    // ========================================================================
    // Messages list context menu + double-click
    // ========================================================================

    private void SetupMessagesListHandlers()
    {
        messagesListBox.ContextRequested += (s, e) =>
        {
            e.Handled = true;
            ShowMessagesContextMenu(e);
        };

        messagesListBox.DoubleTapped += (_, _) =>
        {
            if (ViewModel == null) return;
            int idx = messagesListBox.SelectedIndex;
            if (idx < 0 || idx >= ViewModel.MessageHistory.Count) return;
            var msg = ViewModel.MessageHistory[idx];
            var links = msg.Message.GetLinks();
            if (links != null && links.Length == 1)
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(links[0]) { UseShellExecute = true }); }
                catch { }
            }
        };
    }

    private void ShowMessagesContextMenu(ContextRequestedEventArgs e)
    {
        if (ViewModel == null)
            return;

        SelectListBoxItemFromEvent(messagesListBox, e);

        if (messagesListBox.SelectedIndex < 0 || messagesListBox.SelectedIndex >= ViewModel.MessageHistory.Count)
            return;

        var msg = ViewModel.MessageHistory[messagesListBox.SelectedIndex];

        var contextMenu = new ContextMenu();

        if (!string.IsNullOrEmpty(msg.SenderName))
        {
            var pmItem = new MenuItem { Header = "Private Message" };
            pmItem.Click += (_, _) => ViewModel.OpenSelectedMessageSenderPrivateMessageCommand.Execute(null);
            contextMenu.Items.Add(pmItem);

            contextMenu.Items.Add(new Separator());

            var friendItem = new MenuItem { Header = "Toggle Friend" };
            friendItem.Click += (_, _) => ViewModel.ToggleSelectedMessageSenderFriendCommand.Execute(null);
            contextMenu.Items.Add(friendItem);

            var blockItem = new MenuItem { Header = "Toggle Block" };
            blockItem.Click += (_, _) => ViewModel.ToggleSelectedMessageSenderIgnoreCommand.Execute(null);
            contextMenu.Items.Add(blockItem);

            contextMenu.Items.Add(new Separator());

            var joinItem = new MenuItem { Header = "Join Game" };
            joinItem.Click += (_, _) => ViewModel.JoinSelectedMessageSenderGameCommand.Execute(null);
            contextMenu.Items.Add(joinItem);
        }

        // Link operations from message body
        var links = msg.Message.GetLinks();
        if (links != null && links.Length > 0)
        {
            if (contextMenu.Items.Count > 0)
                contextMenu.Items.Add(new Separator());

            foreach (string link in links)
            {
                string displayLink = link.Length > 40 ? link[..30] + "..." + link[^5..] : link;

                var openLinkItem = new MenuItem { Header = $"Open Link: {displayLink}" };
                openLinkItem.Click += (_, _) =>
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(link) { UseShellExecute = true }); }
                    catch { }
                };
                contextMenu.Items.Add(openLinkItem);

                var copyLinkItem = new MenuItem { Header = $"Copy Link: {displayLink}" };
                copyLinkItem.Click += (_, _) =>
                {
                    try { TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(link); }
                    catch { }
                };
                contextMenu.Items.Add(copyLinkItem);
            }
        }

        if (contextMenu.Items.Count > 0)
            contextMenu.Open(messagesListBox);
    }

    // ========================================================================
    // Recent players context menu
    // ========================================================================

    private void SetupRecentPlayersContextMenu()
    {
        recentPlayersListBox.ContextRequested += (s, e) =>
        {
            e.Handled = true;
            ShowRecentPlayersContextMenu(e);
        };
    }

    private void ShowRecentPlayersContextMenu(ContextRequestedEventArgs e)
    {
        if (ViewModel == null)
            return;

        SelectListBoxItemFromEvent(recentPlayersListBox, e);

        if (recentPlayersListBox.SelectedIndex < 0 || recentPlayersListBox.SelectedIndex >= ViewModel.RecentPlayerNames.Count)
            return;

        var contextMenu = new ContextMenu();

        var pmItem = new MenuItem { Header = "Private Message" };
        pmItem.Click += (_, _) => ViewModel.OpenSelectedRecentPlayerPrivateMessageCommand.Execute(null);
        contextMenu.Items.Add(pmItem);

        contextMenu.Items.Add(new Separator());

        var friendItem = new MenuItem { Header = "Toggle Friend" };
        friendItem.Click += (_, _) => ViewModel.ToggleSelectedRecentPlayerFriendCommand.Execute(null);
        contextMenu.Items.Add(friendItem);

        var blockItem = new MenuItem { Header = "Toggle Block" };
        blockItem.Click += (_, _) => ViewModel.ToggleSelectedRecentPlayerIgnoreCommand.Execute(null);
        contextMenu.Items.Add(blockItem);

        contextMenu.Items.Add(new Separator());

        var joinItem = new MenuItem { Header = "Join Game" };
        joinItem.Click += (_, _) => ViewModel.JoinSelectedRecentPlayerGameCommand.Execute(null);
        contextMenu.Items.Add(joinItem);

        contextMenu.Open(recentPlayersListBox);
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
