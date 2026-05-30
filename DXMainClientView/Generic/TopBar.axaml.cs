using DXMainClientMvvmContract.Generic;

using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Animation;
using Avalonia.Animation.Easings;

namespace DXMainClientView.Generic;

public partial class TopBar : UserControl, ITopBarView
{
    private ITopBarViewModel? _viewModel;
    private readonly TranslateTransform _slideTransform;

    public TopBar()
    {
        InitializeComponent();

        _slideTransform = new TranslateTransform(0, -39);
        RenderTransform = _slideTransform;

        PointerMoved += OnPointerMoved;
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
        _slideTransform.Y = isExpanded ? 0 : -39;
    }
}
