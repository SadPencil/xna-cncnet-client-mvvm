using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
    public void ApplyLayout(Control control, string sectionName)
    {
        string iniPath = FindIniFile(sectionName);
        if (iniPath == null)
        {
            Logger.Log($"INI Layout: No INI file found for '{sectionName}'");
            return;
        }

        Logger.Log($"INI Layout: Loading {iniPath}");
        var iniFile = new CCIniFile(iniPath);

        // Merge base INI at key level (fills in missing keys like
        // IdleTexture, HoverTexture, Location that theme files don't define).
        string basePath = FindBaseIniFile(sectionName);
        if (basePath != null && !string.Equals(basePath, iniPath, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Log($"INI Layout: Merging base {sectionName}.ini from {basePath}");
            var baseIni = new CCIniFile(basePath);
            MergeMissingKeys(baseIni, iniFile);
        }

        // Merge GenericWindow.ini (theme overrides base for window chrome).
        string baseGwPath = FindBaseIniFile("GenericWindow");
        string themeGwPath = FindThemeIniFile("GenericWindow");
        if (themeGwPath != null && baseGwPath != null &&
            !string.Equals(themeGwPath, baseGwPath, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Log($"INI Layout: Merging GenericWindow.ini (base={baseGwPath}, theme={themeGwPath})");
            var baseGwIni = new CCIniFile(baseGwPath);
            var themeGwIni = new CCIniFile(themeGwPath);
            IniFile.ConsolidateIniFiles(baseGwIni, themeGwIni); // theme overrides base
            MergeMissingKeys(baseGwIni, iniFile);
        }
        else
        {
            string gwPath = baseGwPath ?? themeGwPath;
            if (gwPath != null)
            {
                Logger.Log($"INI Layout: Merging GenericWindow.ini from {gwPath}");
                var gwIni = new CCIniFile(gwPath);
                MergeMissingKeys(gwIni, iniFile);
            }
        }

        // Apply root-level properties. Only inherit [GenericWindow] defaults
        // if the main section explicitly references it via $BaseSection.
        // This matches the original XNA behavior where only INItializableWindow
        // subclasses inherit from GenericWindow; XNAWindow-based windows (MainMenu, etc.)
        // do NOT inherit GenericWindow's BackgroundTexture.
        var mainSection = iniFile.GetSection(sectionName);
        var genericSection = iniFile.GetSection("GenericWindow");

        bool inheritsGeneric = false;
        if (mainSection != null)
        {
            string baseSection = mainSection.GetStringValue("$BaseSection", string.Empty);
            inheritsGeneric = string.Equals(baseSection, "GenericWindow", StringComparison.OrdinalIgnoreCase);
        }

        if (inheritsGeneric && genericSection != null)
            ApplyProperties(control, genericSection, iniFile, "GenericWindow");

        if (mainSection != null)
            ApplyProperties(control, mainSection, iniFile, sectionName);

        // Apply properties to all named child controls
        ApplyToDescendants(control, iniFile);

        // Create ExtraControls
        CreateExtraControls(control, iniFile);

        // Apply deferred properties (FillWidth, FillHeight, DistanceFrom*)
        ApplyDeferredProperties(control, iniFile);

        // Auto-load standard {width}pxbtn.png / {width}pxbtn_c.png textures for
        // buttons that weren't given a custom IdleTexture via INI.  This matches
        // XNAClientButton.Initialize() which loads these textures based on Width.
        ApplyStandardButtonTextures(control);

        Logger.Log($"INI Layout: Applied layout for '{sectionName}'");
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

    private static string FindBaseIniFile(string windowName)
    {
        string basePath = ProgramConstants.GetBaseResourcePath();
        string path = Path.Combine(basePath, $"{windowName}.ini");
        return File.Exists(path) ? path : null;
    }

    private static string FindThemeIniFile(string windowName)
    {
        string resourcePath = ProgramConstants.GetResourcePath();
        string path = Path.Combine(resourcePath, $"{windowName}.ini");
        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// Merges all keys from source into target, but only for keys that
    /// don't already exist in the target section. This preserves
    /// theme-specific overrides while filling in base defaults.
    /// </summary>
    private static void MergeMissingKeys(IniFile source, IniFile target)
    {
        foreach (string sectionName in source.GetSections())
        {
            var srcSection = source.GetSection(sectionName);
            var tgtSection = target.GetSection(sectionName);

            if (tgtSection == null)
            {
                // Section doesn't exist in target - use SetStringValue to create it
                foreach (var kvp in srcSection.Keys)
                    target.SetStringValue(sectionName, kvp.Key, kvp.Value);
                continue;
            }

            foreach (var kvp in srcSection.Keys)
            {
                if (tgtSection.KeyExists(kvp.Key))
                    continue;
                tgtSection.SetStringValue(kvp.Key, kvp.Value);
            }
        }
    }

    private static void ApplyToDescendants(Control root, CCIniFile iniFile)
    {
        // Walk all named descendants and apply their INI sections
        ApplyToDescendantsRecursive(root, iniFile);
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
        string? idleTexturePath = null;

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

            // Track IdleTexture path for hover auto-derivation
            if (key == "IdleTexture")
                idleTexturePath = value;
        }

        // Auto-derive HoverTexture for buttons that have IdleTexture but no
        // HoverTexture in the INI section.  Matches original XNA behavior where
        // MainMenu.cs explicitly sets HoverTexture for ALL buttons using the
        // {name}_c.png convention.
        if (control is Button button
            && idleTexturePath != null
            && !section.Keys.Exists(kvp => kvp.Key == "HoverTexture"))
        {
            string hoverPath = DeriveHoverTexturePath(idleTexturePath);
            if (hoverPath != null)
            {
                Logger.Log($"INI Layout: Auto-derived HoverTexture '{hoverPath}' for '{controlName}'");
                ApplyButtonTexture(control, hoverPath, isHover: true);
            }
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

            case "IdleTexture":
                ApplyButtonTexture(control, value, isHover: false);
                break;

            case "HoverTexture":
                ApplyButtonTexture(control, value, isHover: true);
                break;

            case "ForeColor":
            case "TextColorIdle":
                if (control is Button btn && ParseColor(value) is Color fc)
                    btn.Foreground = new SolidColorBrush(fc);
                else if (control is TextBlock tbf && ParseColor(value) is Color tfc)
                    tbf.Foreground = new SolidColorBrush(tfc);
                break;

            case "FontIndex":
                if (control is TextBlock tbFont && int.TryParse(value, out int fontIdx) && fontIdx == 1)
                    tbFont.FontWeight = FontWeight.Bold;
                else if (control is Button btnFont && int.TryParse(value, out int bFontIdx) && bFontIdx == 1)
                    btnFont.FontWeight = FontWeight.Bold;
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

    private static void ApplyDeferredProperties(Control root, CCIniFile iniFile)
    {
        ApplyDeferredRecursive(root, iniFile);
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
            double controlWidth = double.IsNaN(control.Width) ? 0 : control.Width;
            double x = parentWidth - controlWidth - distRight.Value;
            Canvas.SetLeft(control, x);
        }

        // Apply DistanceFromBottomBorder (sets Y based on bottom edge)
        if (distBottom.HasValue && parentHeight > 0)
        {
            double controlHeight = double.IsNaN(control.Height) ? 0 : control.Height;
            double y = parentHeight - controlHeight - distBottom.Value;
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

    private static void CreateExtraControls(Control root, CCIniFile iniFile)
    {
        var hostPanel = FindFirstPanel(root);
        if (hostPanel == null)
            return;

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
                if (FindControlByName(root, controlName) != null)
                    continue;

                var control = CreateControl(controlType, controlName);
                if (control != null)
                {
                    // Apply INI properties
                    var section = iniFile.GetSection(controlName);
                    if (section != null)
                        ApplyProperties(control, section, iniFile, controlName);

                    hostPanel.Children.Add(control);
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

                if (FindControlByName(root, controlName) != null)
                    continue;

                var control = CreateControl(controlType, controlName);
                if (control != null)
                {
                    var section = iniFile.GetSection(controlName);
                    if (section != null)
                        ApplyProperties(control, section, iniFile, controlName);

                    hostPanel.Children.Add(control);
                }
            }
        }
    }

    private static Panel FindFirstPanel(Control control)
    {
        if (control is Panel panel)
            return panel;

        foreach (var child in GetChildren(control))
        {
            var found = FindFirstPanel(child);
            if (found != null)
                return found;
        }

        return null;
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
            string fullPath = FindTextureFileStatic(texturePath);
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
                Stretch = Stretch.Fill,
                TileMode = TileMode.None
            };

            if (control is Window window)
                window.Background = brush;
            else if (control is Border border)
                border.Background = brush;
            else if (control is Panel panel)
                panel.Background = brush;
            else if (control is Button button)
                button.Background = brush;
            else if (control is TemplatedControl templated)
                templated.Background = brush;
            else if (control is Image image)
                image.Source = bitmap;
        }
        catch (Exception ex)
        {
            Logger.Log($"INI Layout: Failed to load texture '{texturePath}': {ex.Message}");
        }
    }

    public string FindTextureFile(string texturePath) => FindTextureFileStatic(texturePath);

    private static string FindTextureFileStatic(string texturePath)
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

    /// <summary>
    /// Derives the hover texture path from an idle texture path using the
    /// {name}_c.{ext} convention (e.g. "MainMenu/campaign.png" → "MainMenu/campaign_c.png").
    /// Returns null if the derived path does not exist on disk.
    /// </summary>
    private static string? DeriveHoverTexturePath(string idleTexturePath)
    {
        string dir = Path.GetDirectoryName(idleTexturePath) ?? string.Empty;
        string name = Path.GetFileNameWithoutExtension(idleTexturePath);
        string ext = Path.GetExtension(idleTexturePath);
        string hoverName = $"{name}_c{ext}";
        string hoverPath = string.IsNullOrEmpty(dir) ? hoverName : Path.Combine(dir, hoverName);
        return FindTextureFileStatic(hoverPath);
    }

    /// <summary>
    /// Walks all descendant Button controls and auto-loads standard {width}pxbtn.png
    /// textures for buttons that weren't given a custom Background via INI IdleTexture.
    /// Matches XNAClientButton.Initialize() behavior.
    /// </summary>
    private static void ApplyStandardButtonTextures(Control root)
    {
        ApplyStandardButtonTexturesRecursive(root);
    }

    private static void ApplyStandardButtonTexturesRecursive(Control parent)
    {
        IEnumerable<Control> children = GetChildren(parent);
        foreach (var child in children)
        {
            if (child is Button button
                && button.Content != null
                && button.Background is not ImageBrush)
            {
                // Determine width: try explicit Width first, then fall back to 133
                int w = !double.IsNaN(button.Width) && button.Width > 0
                    ? (int)button.Width
                    : 133;

                string idlePath = $"{w}pxbtn.png";
                string hoverPath = $"{w}pxbtn_c.png";

                string? idleFile = FindTextureFileStatic(idlePath);
                if (idleFile != null)
                {
                    var idleBitmap = new Bitmap(idleFile);
                    var idleBrush = new ImageBrush
                    {
                        Source = idleBitmap,
                        Stretch = Stretch.Fill,
                        TileMode = TileMode.None
                    };
                    button.Background = idleBrush;

                    // Set up hover texture
                    string? hoverFile = FindTextureFileStatic(hoverPath);
                    if (hoverFile != null)
                    {
                        var hoverBitmap = new Bitmap(hoverFile);
                        var hoverBrush = new ImageBrush
                        {
                            Source = hoverBitmap,
                            Stretch = Stretch.Fill,
                            TileMode = TileMode.None
                        };
                        button.PointerEntered += (_, _) => button.Background = hoverBrush;
                        button.PointerExited += (_, _) => button.Background = idleBrush;
                    }

                    // Text color change on hover (matching XNA TextColorIdle → TextColorHover)
                    var idleForeground = button.Foreground;
                    var hoverColor = ParseColorFromConfig("ButtonHoverColor")
                        ?? Color.Parse("#FCFCFC");
                    var hoverForeground = new SolidColorBrush(hoverColor);
                    button.PointerEntered += (_, _) => button.Foreground = hoverForeground;
                    button.PointerExited += (_, _) =>
                    {
                        if (idleForeground != null)
                            button.Foreground = idleForeground;
                    };
                }
            }

            ApplyStandardButtonTexturesRecursive(child);
        }
    }

    private static Color? ParseColorFromConfig(string key)
    {
        try
        {
            string themePath = FindThemeIniFile("DTACnCNetClient");
            if (themePath != null)
            {
                var ini = new CCIniFile(themePath);
                string? value = ini.GetStringValue("General", key, null);
                if (value != null)
                    return ParseColor(value);
            }
        }
        catch { }
        return null;
    }

    private static void ApplyButtonTexture(Control control, string texturePath, bool isHover)
    {
        if (control is not Button button)
            return;

        try
        {
            string fullPath = FindTextureFileStatic(texturePath);
            if (fullPath == null)
            {
                Logger.Log($"INI Layout: Button texture not found: '{texturePath}'");
                return;
            }

            var bitmap = new Bitmap(fullPath);
            var brush = new ImageBrush
            {
                Source = bitmap,
                Stretch = Stretch.Fill,
                TileMode = TileMode.None
            };

            if (isHover)
            {
                // Capture current background (may be IdleTexture's ImageBrush or style default)
                var idleBrush = button.Background;
                button.PointerEntered += (_, _) => button.Background = brush;
                button.PointerExited += (_, _) =>
                {
                    if (idleBrush != null)
                        button.Background = idleBrush;
                };
            }
            else
            {
                button.Background = brush;
                // Auto-size from texture if no explicit size set
                // Avalonia uses NaN for unset dimensions, not 0
                if (double.IsNaN(button.Width) && double.IsNaN(button.Height))
                {
                    button.Width = bitmap.PixelSize.Width;
                    button.Height = bitmap.PixelSize.Height;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"INI Layout: Failed to load button texture '{texturePath}': {ex.Message}");
        }
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
        else if (control is TemplatedControl templated && templated.Background is ImageBrush templatedBrush)
            brush = templatedBrush;

        if (brush != null)
        {
            brush.Stretch = drawMode?.ToLower() switch
            {
                "stretched" => Stretch.Fill,
                "centered" => Stretch.None,
                "tiled" => Stretch.None,
                _ => Stretch.Fill
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
