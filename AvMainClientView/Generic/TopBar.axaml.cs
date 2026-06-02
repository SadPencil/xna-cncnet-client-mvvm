using AvMainClientMvvmContract.Generic;

using System;
using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace AvMainClientView.Generic;

public partial class TopBar : UserControl, ITopBarView
{
    private ITopBarViewModel? _viewModel;
    private readonly TranslateTransform _slideTransform;
    private DispatcherTimer? _clockTimer;

    public TopBar()
    {
        InitializeComponent();

        _slideTransform = new TranslateTransform(0, -39);
        RenderTransform = _slideTransform;

        PointerMoved += OnPointerMoved;

        // Start clock timer for time display (matching original: TopBar shows time)
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += OnClockTick;
        _clockTimer.Start();
        UpdateTimeDisplay();
    }

    private void OnClockTick(object? sender, EventArgs e)
    {
        UpdateTimeDisplay();
    }

    private void UpdateTimeDisplay()
    {
        if (lblTime != null)
        {
            var now = DateTime.Now;
            lblTime.Text = now.ToString("HH:mm");
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        _viewModel?.ExpandCommand.Execute(null);
    }

    public ITopBarViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModel = value;
            DataContext = value;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                UpdateExpandedState(_viewModel.IsExpanded);
                UpdateUnreadBadge(_viewModel.UnreadMessageCount);
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel == null) return;

        switch (e.PropertyName)
        {
            case nameof(ITopBarViewModel.IsExpanded):
                Dispatcher.UIThread.Post(() => UpdateExpandedState(_viewModel.IsExpanded));
                break;
            case nameof(ITopBarViewModel.UnreadMessageCount):
                Dispatcher.UIThread.Post(() => UpdateUnreadBadge(_viewModel.UnreadMessageCount));
                break;
        }
    }

    private void UpdateExpandedState(bool isExpanded)
    {
        _slideTransform.Y = isExpanded ? 0 : -39;
    }

    private void UpdateUnreadBadge(int count)
    {
        if (unreadBadge != null)
            unreadBadge.IsVisible = count > 0;
        if (lblUnreadCount != null)
            lblUnreadCount.Text = count.ToString();
    }
}
