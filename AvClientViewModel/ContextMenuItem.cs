using System.Windows.Input;

using AvClientMvvmContract;

namespace AvClientViewModel;

/// <summary>
/// Default implementation of IContextMenuItem.
/// </summary>
public record ContextMenuItem(
    string Text,
    ICommand? Command = null,
    bool IsVisible = true,
    bool IsEnabled = true,
    bool IsSeparator = false
) : IContextMenuItem;
