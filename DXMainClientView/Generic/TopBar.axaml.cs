using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DXMainClientViewModel.Generic;

namespace DXMainClientView.Generic;

public partial class TopBar : UserControl, ITopBarView
{
    private ITopBarViewModel? _viewModel;
    private TranslateTransform? _slideTransform;

    public TopBar()
    {
        InitializeComponent();
        _slideTransform = RenderTransform as TranslateTransform;
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
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ITopBarViewModel.IsExpanded) && _viewModel != null)
        {
            UpdateExpandedState(_viewModel.IsExpanded);
        }
    }

    private void UpdateExpandedState(bool isExpanded)
    {
        if (_slideTransform != null)
            _slideTransform.Y = isExpanded ? 0 : -39;
    }
}
