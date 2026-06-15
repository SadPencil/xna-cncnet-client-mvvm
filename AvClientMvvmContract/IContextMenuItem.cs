using System.Windows.Input;

namespace AvClientMvvmContract;

/// <summary>
/// Describes a single item in a context menu.
/// The ViewModel builds the complete list; the View renders it blindly.
/// </summary>
public interface IContextMenuItem
{
    /// <summary>Already-localized display text.</summary>
    string Text { get; }

    /// <summary>Command to execute on click. Null for separators.</summary>
    ICommand? Command { get; }

    /// <summary>Whether this item should be rendered.</summary>
    bool IsVisible { get; }

    /// <summary>Whether this item is clickable.</summary>
    bool IsEnabled { get; }

    /// <summary>True if this is a separator line, not a clickable item.</summary>
    bool IsSeparator { get; }
}
