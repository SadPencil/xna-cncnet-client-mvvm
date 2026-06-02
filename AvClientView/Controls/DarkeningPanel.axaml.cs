using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace AvClientView.Controls;

/// <summary>
/// A full-screen semi-transparent overlay that centers its child content.
/// Use IsPanelVisible to control show/hide (bind to ViewModel property).
/// Set Content in AXAML or code-behind.
/// </summary>
public partial class DarkeningPanel : UserControl
{
    public static readonly StyledProperty<bool> IsPanelVisibleProperty =
        AvaloniaProperty.Register<DarkeningPanel, bool>(nameof(IsPanelVisible));

    public static readonly StyledProperty<Control?> ChildContentProperty =
        AvaloniaProperty.Register<DarkeningPanel, Control?>(nameof(ChildContent));

    public bool IsPanelVisible
    {
        get => GetValue(IsPanelVisibleProperty);
        set => SetValue(IsPanelVisibleProperty, value);
    }

    public Control? ChildContent
    {
        get => GetValue(ChildContentProperty);
        set => SetValue(ChildContentProperty, value);
    }

    public DarkeningPanel()
    {
        InitializeComponent();
        ChildContentProperty.Changed.AddClassHandler<DarkeningPanel>((x, e) =>
        {
            if (e.NewValue is Control control)
                x.PART_ContentPresenter.Content = control;
        });
        IsPanelVisibleProperty.Changed.AddClassHandler<DarkeningPanel>((x, e) =>
        {
            x.IsVisible = e.NewValue is true;
        });
    }
}
