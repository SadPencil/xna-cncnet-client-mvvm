using Avalonia;
using Avalonia.Controls;

using Serilog;

namespace AvClientView.Services;

/// <summary>
/// Attached properties that controls can set to opt out of specific
/// INI layout overrides.  Useful for ComboBoxes whose item templates
/// set their own Foreground (e.g. colour-picker dropdowns).
///
/// Sets the "skip-foreground" CSS class so the Application style
/// can match <c>ComboBox.skip-foreground</c>.
/// </summary>
public static class IniLayoutProperties
{
    public const string SkipForegroundClass = "skip-foreground";

    public static readonly AttachedProperty<bool> SkipForegroundProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "SkipForeground",
            typeof(IniLayoutProperties),
            defaultValue: false,
            inherits: false);

    public static bool GetSkipForeground(Control control) =>
        control.GetValue(SkipForegroundProperty);

    public static void SetSkipForeground(Control control, bool value)
    {
        control.SetValue(SkipForegroundProperty, value);
        if (value)
            control.Classes.Add(SkipForegroundClass);
        else
            control.Classes.Remove(SkipForegroundClass);
        Log.Information("[LOG-SkipFG] {Control} '{Name}': SkipForeground={Val}, Classes={Classes}",
            control.GetType().Name, control.Name, value, string.Join(",", control.Classes));
    }
}
