using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ClientCore;
using Rampastring.Tools;

namespace DXMainClientView.Services;

/// <summary>
/// Reads INI files and applies layout properties to Avalonia controls.
/// Supports the same property system as XNAUI: Size, Location, BackgroundTexture,
/// DistanceFromRightBorder, FillWidth, ExtraControls, etc.
/// </summary>
public class IniLayoutOverlayService : IIniLayoutOverlayService
{
    public void ApplyLayout(Window window, string windowName)
    {
        string iniPath = FindIniFile(windowName);
        if (iniPath == null)
        {
            Logger.Log($"INI Layout: No INI file found for '{windowName}'");
            return;
        }

        Logger.Log($"INI Layout: Loading {iniPath}");
        var iniFile = new CCIniFile(iniPath);

        // Apply window-level properties
        var windowSection = iniFile.GetSection(windowName)
                         ?? iniFile.GetSection("GenericWindow");
        if (windowSection != null)
            ApplyProperties(window, windowSection, iniFile, windowName);

        // Apply properties to all named child controls
        ApplyToDescendants(window, iniFile);

        // Create ExtraControls
        CreateExtraControls(window, iniFile);

        // Apply deferred properties (FillWidth, FillHeight, DistanceFrom*)
        ApplyDeferredProperties(window, iniFile);

        Logger.Log($"INI Layout: Applied layout for '{windowName}'");
    }

    private static string FindIniFile(string windowName)
    {
        // Search order (matching XNAWindow.SetAttributesFromIni):
        // 1. Theme-specific path: {ResourcePath}/{windowName}.ini
        // 2. Base path: {BaseResourcePath}/{windowName}.ini
        // 3. Theme GenericWindow.ini
        // 4. Base GenericWindow.ini

        string resourcePath = ProgramConstants.GetResourcePath();
        string basePath = ProgramConstants.GetBaseResourcePath();

        string themeSpecific = Path.Combine(resourcePath, $"{windowName}.ini");
        if (File.Exists(themeSpecific))
            return themeSpecific;

        string baseSpecific = Path.Combine(basePath, $"{windowName}.ini");
        if (File.Exists(baseSpecific))
            return baseSpecific;

        string themeGeneric = Path.Combine(resourcePath, "GenericWindow.ini");
        if (File.Exists(themeGeneric))
            return themeGeneric;

        string baseGeneric = Path.Combine(basePath, "GenericWindow.ini");
        if (File.Exists(baseGeneric))
            return baseGeneric;

        return null;
    }

    private static void ApplyToDescendants(Window window, CCIniFile iniFile)
    {
        // Walk all named descendants and apply their INI sections
        ApplyToDescendantsRecursive(window, iniFile);
    }

    private static void ApplyToDescendantsRecursive(Control parent, CCIniFile iniFile)
    {
        if (parent is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Control control && !string.IsNullOrEmpty(control.Name))
                {
                    var section = iniFile.GetSection(control.Name);
                    if (section != null)
                        ApplyProperties(control, section, iniFile, control.Name);
                }
                ApplyToDescendantsRecursive(child as Control ?? parent, iniFile);
            }
        }
        else if (parent is ContentControl contentControl && contentControl.Content is Control contentChild)
        {
            if (!string.IsNullOrEmpty(contentChild.Name))
            {
                var section = iniFile.GetSection(contentChild.Name);
                if (section != null)
                    ApplyProperties(contentChild, section, iniFile, contentChild.Name);
            }
            ApplyToDescendantsRecursive(contentChild, iniFile);
        }
        else if (parent is Decorator decorator && decorator.Child is Control decoratorChild)
        {
            if (!string.IsNullOrEmpty(decoratorChild.Name))
            {
                var section = iniFile.GetSection(decoratorChild.Name);
                if (section != null)
                    ApplyProperties(decoratorChild, section, iniFile, decoratorChild.Name);
            }
            ApplyToDescendantsRecursive(decoratorChild, iniFile);
        }
    }

    private static void ApplyProperties(Control control, IniSection section, CCIniFile iniFile, string controlName)
    {
        foreach (var kvp in section.Keys)
        {
            string key = kvp.Key;
            string value = kvp.Value;

            // Skip $BaseSection (handled by CCIniFile)
            if (key == "$BaseSection")
                continue;

            // Skip $CC keys (handled by CreateExtraControls)
            if (key.StartsWith("$CC"))
                continue;

            ApplySingleProperty(control, key, value, controlName);
        }
    }

    private static void ApplySingleProperty(Control control, string key, string value, string controlName)
    {
        switch (key)
        {
            case "Size":
                var sizeParts = value.Split(',');
                if (sizeParts.Length == 2)
                {
                    if (int.TryParse(sizeParts[0], out int w))
                        control.Width = w;
                    if (int.TryParse(sizeParts[1], out int h))
                        control.Height = h;
                }
                break;

            case "Location":
                var locParts = value.Split(',');
                if (locParts.Length == 2)
                {
                    if (int.TryParse(locParts[0], out int x))
                        Canvas.SetLeft(control, x);
                    if (int.TryParse(locParts[1], out int y))
                        Canvas.SetTop(control, y);
                }
                break;

            case "X":
                if (int.TryParse(value, out int xVal))
                    Canvas.SetLeft(control, xVal);
                break;

            case "Y":
                if (int.TryParse(value, out int yVal))
                    Canvas.SetTop(control, yVal);
                break;

            case "Width":
                if (int.TryParse(value, out int wVal))
                    control.Width = wVal;
                break;

            case "Height":
                if (int.TryParse(value, out int hVal))
                    control.Height = hVal;
                break;

            case "Visible":
                control.IsVisible = ParseBool(value);
                break;

            case "Enabled":
                control.IsEnabled = ParseBool(value);
                break;

            case "DrawBorders":
                if (control is Border border)
                    border.BorderThickness = ParseBool(value) ? new Thickness(1) : new Thickness(0);
                break;

            case "BackgroundTexture":
                ApplyBackgroundTexture(control, value);
                break;

            case "DrawMode":
                ApplyDrawMode(control, value);
                break;

            case "RepeatingImage":
                // RepeatingImage=false means stretch, true means tile
                if (!ParseBool(value))
                    ApplyDrawMode(control, "Stretched");
                else
                    ApplyDrawMode(control, "Tiled");
                break;

            case "RemapColor":
                ApplyRemapColor(control, value);
                break;

            case "Text":
                if (control is TextBlock textBlock)
                    textBlock.Text = value.Replace("@", "\n");
                else if (control is Button button)
                    button.Content = value.Replace("@", "\n");
                else if (control is TextBox textBox)
                    textBox.Text = value.Replace("@", "\n");
                break;

            case "TextColor":
                if (control is TextBlock tb && ParseColor(value) is Color tc)
                    tb.Foreground = new SolidColorBrush(tc);
                break;

            case "ToolTip":
                ToolTip.SetTip(control, value.Replace("@", "\n"));
                break;

            // Deferred properties - store for later processing
            case "FillWidth":
            case "FillHeight":
            case "DistanceFromRightBorder":
            case "DistanceFromBottomBorder":
            case "DistanceFromLeftBorder":
            case "DistanceFromTopBorder":
                // These are handled in ApplyDeferredProperties
                break;

            // Skip properties we don't handle yet
            default:
                break;
        }
    }

    private static void ApplyDeferredProperties(Window window, CCIniFile iniFile)
    {
        ApplyDeferredRecursive(window, iniFile);
    }

    private static void ApplyDeferredRecursive(Control parent, CCIniFile iniFile)
    {
        // Process named children
        IEnumerable<Control> children = GetChildren(parent);

        foreach (var child in children)
        {
            if (!string.IsNullOrEmpty(child.Name))
            {
                var section = iniFile.GetSection(child.Name);
                if (section != null)
                    ApplyDeferredToControl(child, section, parent);
            }
            ApplyDeferredRecursive(child, iniFile);
        }
    }

    private static void ApplyDeferredToControl(Control control, IniSection section, Control parent)
    {
        double? fillWidth = null;
        double? fillHeight = null;
        double? distRight = null;
        double? distBottom = null;
        double? distLeft = null;
        double? distTop = null;

        foreach (var kvp in section.Keys)
        {
            switch (kvp.Key)
            {
                case "FillWidth":
                    if (int.TryParse(kvp.Value, out int fw))
                        fillWidth = fw;
                    break;
                case "FillHeight":
                    if (int.TryParse(kvp.Value, out int fh))
                        fillHeight = fh;
                    break;
                case "DistanceFromRightBorder":
                    if (int.TryParse(kvp.Value, out int dr))
                        distRight = dr;
                    break;
                case "DistanceFromBottomBorder":
                    if (int.TryParse(kvp.Value, out int db))
                        distBottom = db;
                    break;
                case "DistanceFromLeftBorder":
                    if (int.TryParse(kvp.Value, out int dl))
                        distLeft = dl;
                    break;
                case "DistanceFromTopBorder":
                    if (int.TryParse(kvp.Value, out int dt))
                        distTop = dt;
                    break;
            }
        }

        // Find the nearest ancestor with explicit dimensions (Window or sized control).
        // Canvas/Panel may not have explicit Width/Height, so walk up to the Window.
        double parentWidth = GetEffectiveWidth(parent);
        double parentHeight = GetEffectiveHeight(parent);

        // Apply DistanceFromLeftBorder first (sets X)
        if (distLeft.HasValue)
            Canvas.SetLeft(control, distLeft.Value);

        // Apply DistanceFromTopBorder (sets Y)
        if (distTop.HasValue)
            Canvas.SetTop(control, distTop.Value);

        // Apply DistanceFromRightBorder (sets X based on right edge)
        if (distRight.HasValue && parentWidth > 0)
        {
            double x = parentWidth - control.Width - distRight.Value;
            Canvas.SetLeft(control, x);
        }

        // Apply DistanceFromBottomBorder (sets Y based on bottom edge)
        if (distBottom.HasValue && parentHeight > 0)
        {
            double y = parentHeight - control.Height - distBottom.Value;
            Canvas.SetTop(control, y);
        }

        // Apply FillWidth (sets width to fill from X to right edge minus value)
        if (fillWidth.HasValue && parentWidth > 0)
        {
            double x = Canvas.GetLeft(control);
            control.Width = parentWidth - x - fillWidth.Value;
        }

        // Apply FillHeight (sets height to fill from Y to bottom edge minus value)
        if (fillHeight.HasValue && parentHeight > 0)
        {
            double y = Canvas.GetTop(control);
            control.Height = parentHeight - y - fillHeight.Value;
        }
    }

    private static void CreateExtraControls(Window window, CCIniFile iniFile)
    {
        // Handle [ExtraControls] section (legacy format: 0=controlName:ControlType)
        var extraSection = iniFile.GetSection("ExtraControls");
        if (extraSection != null)
        {
            foreach (var kvp in extraSection.Keys)
            {
                string[] parts = kvp.Value.Split(':');
                if (parts.Length != 2)
                    continue;

                string controlName = parts[0];
                string controlType = parts[1];

                // Skip if control already exists (from AXAML)
                if (FindControlByName(window, controlName) != null)
                    continue;

                var control = CreateControl(controlType, controlName);
                if (control != null)
                {
                    // Apply INI properties
                    var section = iniFile.GetSection(controlName);
                    if (section != null)
                        ApplyProperties(control, section, iniFile, controlName);

                    // Add to window's content
                    if (window.Content is Panel panel)
                        panel.Children.Add(control);
                }
            }
        }

        // Handle [$ExtraControls] section (new format: $CCXX=controlName:ControlType)
        var extraSection2 = iniFile.GetSection("$ExtraControls");
        if (extraSection2 != null)
        {
            foreach (var kvp in extraSection2.Keys)
            {
                if (!kvp.Key.StartsWith("$CC"))
                    continue;

                string[] parts = kvp.Value.Split(':');
                if (parts.Length != 2)
                    continue;

                string controlName = parts[0];
                string controlType = parts[1];

                if (FindControlByName(window, controlName) != null)
                    continue;

                var control = CreateControl(controlType, controlName);
                if (control != null)
                {
                    var section = iniFile.GetSection(controlName);
                    if (section != null)
                        ApplyProperties(control, section, iniFile, controlName);

                    if (window.Content is Panel panel)
                        panel.Children.Add(control);
                }
            }
        }
    }

    private static Control CreateControl(string controlType, string name)
    {
        // Map XNAUI types to Avalonia controls
        return controlType switch
        {
            "XNAExtraPanel" or "XNAPanel" or "XNAControl" => new Border
            {
                Name = name,
                Child = new Panel()
            },
            "XNALabel" => new TextBlock { Name = name },
            "XNAButton" or "XNAClientButton" => new Button { Name = name },
            "XNACheckBox" or "XNAClientCheckBox" => new CheckBox { Name = name },
            "XNADropDown" or "XNAClientDropDown" => new ComboBox { Name = name },
            "XNATextBox" or "XNASuggestionTextBox" => new TextBox { Name = name },
            _ => null
        };
    }

    private static Control FindControlByName(Control root, string name)
    {
        if (root.Name == name)
            return root;

        foreach (var child in GetChildren(root))
        {
            var found = FindControlByName(child, name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static IEnumerable<Control> GetChildren(Control parent)
    {
        if (parent is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Control c)
                    yield return c;
            }
        }
        else if (parent is ContentControl cc && cc.Content is Control content)
        {
            yield return content;
        }
        else if (parent is Decorator d && d.Child is Control child)
        {
            yield return child;
        }
        else if (parent is Window window && window.Content is Control windowContent)
        {
            yield return windowContent;
        }
    }

    private static void ApplyBackgroundTexture(Control control, string texturePath)
    {
        try
        {
            // Search for the texture in resource paths
            string fullPath = FindTextureFile(texturePath);
            if (fullPath == null)
            {
                Logger.Log($"INI Layout: Texture not found: '{texturePath}'");
                return;
            }

            Logger.Log($"INI Layout: Loading texture '{texturePath}' from {fullPath}");
            var bitmap = new Bitmap(fullPath);

            var brush = new ImageBrush
            {
                Source = bitmap,
                Stretch = Stretch.UniformToFill,
                TileMode = TileMode.None
            };

            if (control is Window window)
                window.Background = brush;
            else if (control is Border border)
                border.Background = brush;
            else if (control is Panel panel)
                panel.Background = brush;
            else if (control is Image image)
                image.Source = bitmap;
        }
        catch (Exception ex)
        {
            Logger.Log($"INI Layout: Failed to load texture '{texturePath}': {ex.Message}");
        }
    }

    private static string FindTextureFile(string texturePath)
    {
        // Search in resource paths (theme first, then base)
        string resourcePath = ProgramConstants.GetResourcePath();
        string basePath = ProgramConstants.GetBaseResourcePath();

        string themePath = Path.Combine(resourcePath, texturePath);
        if (File.Exists(themePath))
            return themePath;

        string basePathFull = Path.Combine(basePath, texturePath);
        if (File.Exists(basePathFull))
            return basePathFull;

        // Try without subdirectory
        string themePathDirect = Path.Combine(resourcePath, Path.GetFileName(texturePath));
        if (File.Exists(themePathDirect))
            return themePathDirect;

        string basePathDirect = Path.Combine(basePath, Path.GetFileName(texturePath));
        if (File.Exists(basePathDirect))
            return basePathDirect;

        return null;
    }

    private static void ApplyDrawMode(Control control, string drawMode)
    {
        ImageBrush brush = null;

        if (control is Window window && window.Background is ImageBrush windowBrush)
            brush = windowBrush;
        else if (control is Border border && border.Background is ImageBrush borderBrush)
            brush = borderBrush;
        else if (control is Panel panel && panel.Background is ImageBrush panelBrush)
            brush = panelBrush;

        if (brush != null)
        {
            brush.Stretch = drawMode?.ToLower() switch
            {
                "stretched" => Stretch.UniformToFill,
                "centered" => Stretch.None,
                "tiled" => Stretch.None,
                _ => Stretch.UniformToFill
            };

            if (drawMode?.ToLower() == "tiled")
                brush.TileMode = TileMode.FlipXY;
        }
    }

    private static void ApplyRemapColor(Control control, string colorStr)
    {
        var color = ParseColor(colorStr);
        if (color == null)
            return;

        // Apply as opacity based on alpha channel
        if (color.Value.A < 255)
            control.Opacity = color.Value.A / 255.0;
    }

    private static Color? ParseColor(string colorStr)
    {
        if (string.IsNullOrWhiteSpace(colorStr))
            return null;

        string[] parts = colorStr.Split(',');
        if (parts.Length < 3)
            return null;

        if (int.TryParse(parts[0], out int r) &&
            int.TryParse(parts[1], out int g) &&
            int.TryParse(parts[2], out int b))
        {
            int a = 255;
            if (parts.Length >= 4)
            {
                if (int.TryParse(parts[3], out int parsedA))
                    a = parsedA;
            }
            return Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
        }

        return null;
    }

    private static bool ParseBool(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        char first = char.ToLower(value[0]);
        return first == 't' || first == 'y' || first == '1' || first == 'a' || first == 'e';
    }

    /// <summary>
    /// Gets the effective width of a control, walking up to the Window if needed.
    /// Canvas/Panel may not have explicit Width, so we use the Window's Width.
    /// </summary>
    private static double GetEffectiveWidth(Control control)
    {
        var current = control;
        while (current != null)
        {
            if (!double.IsNaN(current.Width) && current.Width > 0)
                return current.Width;
            if (current is Window window)
                return window.Width > 0 ? window.Width : window.ClientSize.Width;
            current = current.Parent as Control;
        }
        return 0;
    }

    /// <summary>
    /// Gets the effective height of a control, walking up to the Window if needed.
    /// Canvas/Panel may not have explicit Height, so we use the Window's Height.
    /// </summary>
    private static double GetEffectiveHeight(Control control)
    {
        var current = control;
        while (current != null)
        {
            if (!double.IsNaN(current.Height) && current.Height > 0)
                return current.Height;
            if (current is Window window)
                return window.Height > 0 ? window.Height : window.ClientSize.Height;
            current = current.Parent as Control;
        }
        return 0;
    }
}
