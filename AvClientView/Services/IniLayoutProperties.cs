using Avalonia;
using Avalonia.Controls;

namespace AvClientView.Services;

/// <summary>
/// Attached properties that controls can set to opt out of specific
/// INI layout overrides.  Useful for ComboBoxes whose item templates
/// set their own Foreground (e.g. colour-picker dropdowns).
/// </summary>
public static class IniLayoutProperties
{
    /// <summary>
    /// When set to true on a ComboBox, the INI overlay service will NOT
    /// set <c>ComboBox.Foreground</c> (the inherited text colour).
    /// Individual items are still free to choose their own Foreground.
    /// </summary>
    public static readonly AttachedProperty<bool> SkipForegroundProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "SkipForeground",
            typeof(IniLayoutProperties),
            defaultValue: false,
            inherits: false);

    public static bool GetSkipForeground(Control control) =>
        control.GetValue(SkipForegroundProperty);
    public static void SetSkipForeground(Control control, bool value) =>
        control.SetValue(SkipForegroundProperty, value);
}
