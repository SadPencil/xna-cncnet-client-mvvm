using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using ClientCore;

using DXMainClientMvvmContract.ViewServices;

using Rampastring.Tools;

namespace DXMainClientView.Services;

/// <summary>
/// Reads INI files and applies layout properties to Avalonia controls.
/// Supports the same property system as XNAUI: Size, Location, BackgroundTexture,
/// DistanceFromRightBorder, FillWidth, ExtraControls, etc.
/// </summary>
public class IniLayoutOverlayService : IIniLayoutOverlayService
{
    private static IUrlService? _urlService;

    public IniLayoutOverlayService(IUrlService urlService)
    {
        _urlService = urlService;
    }
    /// <summary>
    /// Font configuration for FontIndex values. Maps FontIndex to (FontSize, FontWeight).
    /// XNAUI FontIndex 0 = default font, FontIndex 1 = bold font.
    /// Unknown FontIndex falls back to index 0.
    /// </summary>
    private static readonly Dictionary<int, (double size, FontWeight weight)> FontIndexConfig = new()
    {
        [0] = (12, FontWeight.Normal),
        [1] = (12, FontWeight.Bold),
    };

    /// <summary>
    /// Gets the font size and weight for a given FontIndex.
    /// Falls back to FontIndex 0 for unknown indices.
    /// </summary>
    private static (double size, FontWeight weight) GetFontConfig(int fontIndex)
    {
        if (FontIndexConfig.TryGetValue(fontIndex, out var config))
            return config;
        return FontIndexConfig[0]; // fallback
    }

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

        // Apply hardcoded draw modes from XNA code first (even if INI has no section)
        ApplyHardcodedDrawModes(control);

        // Apply properties to all named child controls (INI DrawMode can override)
        ApplyToDescendants(control, iniFile);

        // Create ExtraControls
        CreateExtraControls(control, iniFile);

        // Apply deferred properties (FillWidth, FillHeight, DistanceFrom*)
        ApplyDeferredProperties(control, iniFile);

        // Debug: dump final state of chrome bar controls
        DumpControlState(control, "leftbar");
        DumpControlState(control, "rightbar");

        // Auto-load standard {width}pxbtn.png / {width}pxbtn_c.png textures for
        // buttons that weren't given a custom IdleTexture via INI.  This matches
        // XNAClientButton.Initialize() which loads these textures based on Width.
        ApplyStandardButtonTextures(control);

        // Apply theme colors from DTACnCNetClient.ini as DynamicResource brushes.
        // AXAML files can reference these via {DynamicResource XnaTextBrush}, etc.
        ApplyThemeColors(control);

        // Apply INI theme colors to ComboBox and TextBox controls.
        // Matches XNADropDown/XNATextBox drawing with BackColor, BorderColor, TextColor.
        ApplyInputControlStyles(control);

        Logger.Log($"INI Layout: Applied layout for '{sectionName}'");
    }

    private static void DumpControlState(Control root, string name)
    {
        var control = FindControlByName(root, name);
        if (control is Border border)
        {
            Logger.Log($"INI Layout: FINAL '{name}': Width={border.Width}, Height={border.Height}, " +
                $"Child={border.Child?.GetType().Name ?? "null"}, " +
                $"Background={border.Background?.GetType().Name ?? "null"}, " +
                $"Bounds={border.Bounds.Width}x{border.Bounds.Height}, " +
                $"Canvas.Left={Canvas.GetLeft(border)}, Canvas.Top={Canvas.GetTop(border)}");
        }
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

            int addedKeys = 0;
            foreach (var kvp in srcSection.Keys)
            {
                if (tgtSection.KeyExists(kvp.Key))
                    continue;
                tgtSection.SetStringValue(kvp.Key, kvp.Value);
                addedKeys++;
            }
            if (sectionName is "leftbar" or "rightbar" or "ExtraControls" && addedKeys > 0)
                Logger.Log($"INI Layout: MergeMissingKeys: merged {addedKeys} keys into existing [{sectionName}]");
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
                // Stop recursing into UserControls - they have their own INI overlay
                if (child is not UserControl)
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
            // Stop recursing into UserControls - they have their own INI overlay
            if (contentChild is not UserControl)
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
            // Stop recursing into UserControls - they have their own INI overlay
            if (decoratorChild is not UserControl)
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

            case "URL":
                // XNALinkButton: open URL/executable on click via IUrlService
                if (control is Button urlButton)
                {
                    string url = value;
                    urlButton.Click += (_, _) =>
                    {
                        try
                        {
                            if (_urlService != null)
                                _urlService.OpenUrl(url);
                            else
                                Logger.Log($"INI Layout: IUrlService not available, cannot open '{url}'");
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"INI Layout: Failed to open URL '{url}': {ex.Message}");
                        }
                    };
                }
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
                if (int.TryParse(value, out int fontIdx))
                {
                    var (fontSize, fontWeight) = GetFontConfig(fontIdx);
                    if (control is TextBlock tbFont)
                    {
                        tbFont.FontSize = fontSize;
                        tbFont.FontWeight = fontWeight;
                    }
                    else if (control is Button btnFont)
                    {
                        btnFont.FontSize = fontSize;
                        btnFont.FontWeight = fontWeight;
                    }
                    else if (control is ComboBox cbFont)
                    {
                        cbFont.FontSize = fontSize;
                        cbFont.FontWeight = fontWeight;
                    }
                    else if (control is TextBox txtFont)
                    {
                        txtFont.FontSize = fontSize;
                        txtFont.FontWeight = fontWeight;
                    }
                    else if (control is CheckBox chkFont)
                    {
                        chkFont.FontSize = fontSize;
                        chkFont.FontWeight = fontWeight;
                    }
                }
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
                // GetSection has a _lastSectionIndex optimization that can skip
                // sections appended at the end by MergeMissingKeys. To work around
                // this, we use FindSectionDirect which detects and resets the cache.
                var section = FindSectionDirect(iniFile, child.Name);
                if (section != null)
                {
                    ApplyDeferredToControl(child, section, parent);
                }
            }
            ApplyDeferredRecursive(child, iniFile);
        }
    }

    /// <summary>
    /// Finds an INI section by direct search, bypassing the _lastSectionIndex
    /// cache that can skip sections appended by MergeMissingKeys.
    /// </summary>
    private static IniSection? FindSectionDirect(IniFile iniFile, string name)
    {
        var section = iniFile.GetSection(name);
        if (section != null)
            return section;

        // GetSection returned null. Check if the section actually exists.
        // This works around the _lastSectionIndex cache bug in IniFile.GetSection
        // that can skip sections appended by MergeMissingKeys.
        var names = iniFile.GetSections();
        if (names.Contains(name))
        {
            // Reset _lastSectionIndex by calling GetSection with a non-existent name.
            iniFile.GetSection("__reset_index__");
            return iniFile.GetSection(name);
        }
        return null;
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
            if (double.IsNaN(x)) x = 0; // fallback if Location X was not set
            control.Width = parentWidth - x - fillWidth.Value;
            control.InvalidateMeasure();
        }

        // Apply FillHeight (sets height to fill from Y to bottom edge minus value)
        if (fillHeight.HasValue && parentHeight > 0)
        {
            double y = Canvas.GetTop(control);
            if (double.IsNaN(y)) y = 0; // fallback if Location Y was not set
            double newHeight = parentHeight - y - fillHeight.Value;
            Logger.Log($"INI Layout: FillHeight for '{control.Name}': parent={parentHeight}, y={y}, fillHeight={fillHeight.Value} → height={newHeight}");

            control.Height = newHeight;
            control.InvalidateMeasure();
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
            int insertIndex = 0;
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

                    // Apply hardcoded draw mode (INI DrawMode can override)
                    ApplyHardcodedDrawMode(control);

                    // Debug: dump state of chrome bar controls after creation
                    if (controlName is "leftbar" or "rightbar" && control is Border dbgBorder)
                    {
                        Logger.Log($"INI Layout: CREATED '{controlName}': Width={dbgBorder.Width}, Height={dbgBorder.Height}, " +
                            $"Child={dbgBorder.Child?.GetType().Name ?? "null"}, " +
                            $"Background={dbgBorder.Background?.GetType().Name ?? "null"}, " +
                            $"Canvas.Left={Canvas.GetLeft(dbgBorder)}, Canvas.Top={Canvas.GetTop(dbgBorder)}");
                    }

                    // Insert at beginning so ExtraControls are below DarkeningPanels
                    hostPanel.Children.Insert(insertIndex, control);
                    insertIndex++;
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
            // Panels - no Child needed for decorative ExtraControls.
            // A Panel child interferes with Border's DesiredSize calculation,
            // causing FillHeight to not render correctly.
            "XNAExtraPanel" or "XNAPanel" or "XNAControl" => new Border
            {
                Name = name,
            },
            // TODO: PlayerExtraOptionsPanel - needs custom template with player slot controls
            "PlayerExtraOptionsPanel" => new Border
            {
                Name = name,
                Child = new Panel()
            },
            // TODO: MapPreviewBox - needs custom control for map preview rendering
            "MapPreviewBox" => new Border
            {
                Name = name,
                Child = new Panel()
            },
            // Labels
            "XNALabel" => new TextBlock { Name = name },
            // Buttons
            "XNAButton" or "XNAClientButton" or "XNALinkButton"
            or "GameLaunchButton" => new Button { Name = name },
            // Checkboxes - basic types
            "XNACheckBox" or "XNAClientCheckBox" => new CheckBox { Name = name },
            // TODO: SettingCheckBox - needs ViewModel with IIniSettingsService for INI read/write
            // Has special attributes: SettingSection, SettingKey, ValueWhenChecked, ValueWhenUnchecked
            // View cannot handle this alone - requires ViewModel property binding
            "SettingCheckBox" => new CheckBox { Name = name },
            // TODO: FileSettingCheckBox - needs ViewModel for file-based settings read/write
            "FileSettingCheckBox" => new CheckBox { Name = name },
            // TODO: CampaignCheckBox - needs ViewModel with campaign mission data binding
            "CampaignCheckBox" => new CheckBox { Name = name },
            // TODO: GameLobbyCheckBox - needs ViewModel with game option binding
            "GameLobbyCheckBox" => new CheckBox { Name = name },
            // Dropdowns - basic types
            "XNADropDown" or "XNAClientDropDown" => new ComboBox { Name = name },
            // TODO: SettingDropDown - needs ViewModel with IIniSettingsService for INI read/write
            "SettingDropDown" => new ComboBox { Name = name },
            // TODO: FileSettingDropDown - needs ViewModel for file-based settings read/write
            "FileSettingDropDown" => new ComboBox { Name = name },
            // Text inputs
            "XNATextBox" or "XNASuggestionTextBox" => new TextBox { Name = name },
            // TODO: XNAMultiColumnListBox - needs custom multi-column list control
            "XNAMultiColumnListBox" => new ListBox { Name = name },
            "XNAListBox" => new ListBox { Name = name },
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

    /// <summary>
    /// Hardcoded draw modes from XNA code (PanelBackgroundDrawMode assignments).
    /// Key: control name, Value: draw mode string.
    /// </summary>
    private static readonly Dictionary<string, string> HardcodedDrawModes = new()
    {
        // CampaignSelector.cs
        ["lbCampaignList"] = "stretched",
        ["tbMissionDescription"] = "stretched",
        ["pnlMissionPreview"] = "stretched",
        // CheaterWindow.cs
        ["imagePanel"] = "stretched",
        // GameInProgressWindow.cs
        ["GameInProgressWindow"] = "stretched",
        // GameLoadingWindow.cs
        ["lbSaveGameList"] = "stretched",
        // UpdaterOptionsPanel.cs
        ["lbUpdateServerList"] = "stretched",
        // StatisticsWindow.cs
        ["lbGameList"] = "stretched",
        ["lbGameStatistics"] = "stretched",
        // TopBar.cs
        ["TopBar"] = "stretched",
        // ChoiceNotificationBox.cs
        ["ChoiceNotificationBox"] = "stretched",
        // CnCNetLobby.cs
        ["CnCNetLobby"] = "stretched",
        // LoadOrSaveGameOptionPresetWindow.cs
        ["LoadOrSaveGameOptionPresetWindow"] = "stretched",
        // MapSharingConfirmationPanel.cs
        ["MapSharingConfirmationPanel"] = "tiled",
        // PrivateMessageNotificationBox.cs
        ["PrivateMessageNotificationBox"] = "stretched",
        // PrivateMessagingWindow.cs
        ["lbUserList"] = "stretched",
        ["lbMessages"] = "stretched",
        // TunnelListBox.cs
        ["TunnelListBox"] = "stretched",
        // TunnelSelectionWindow.cs
        ["TunnelSelectionWindow"] = "stretched",
        // GameInformationPanel.cs
        ["GameInformationPanel"] = "stretched",
        // GameLoadingLobbyBase.cs
        ["panelPlayers"] = "stretched",
        // CoopBriefingBox.cs
        ["CoopBriefingBox"] = "stretched",
        // MapPreviewBox.cs
        ["MapPreviewBox"] = "stretched",
        // LANLobby.cs
        ["LANLobby"] = "stretched",
    };

    /// <summary>
    /// Gets the Stretch and TileMode for a draw mode string.
    /// Shared by ApplyBackgroundTexture and ApplyHardcodedDrawModes.
    /// </summary>
    private static (Stretch stretch, TileMode tileMode) GetDrawModeSettings(string drawMode)
    {
        return drawMode?.ToLower() switch
        {
            // In Avalonia, ImageBrush with TileMode.None renders the image at its
            // natural size — Stretch only affects how the image fits within each tile.
            // We must use TileMode.FlipXY for "stretched" and "centered" so the brush
            // actually scales the image to fill the control area.
            "stretched" => (Stretch.Fill, TileMode.FlipXY),
            "centered" => (Stretch.UniformToFill, TileMode.FlipXY),
            "tiled" => (Stretch.None, TileMode.FlipXY),
            _ => (Stretch.Fill, TileMode.FlipXY)
        };
    }

    /// <summary>
    /// Gets the ImageBrush from a control's Background.
    /// </summary>
    private static ImageBrush? GetImageBrush(Control control)
    {
        if (control is Border border && border.Background is ImageBrush borderBrush)
            return borderBrush;
        if (control is Panel panel && panel.Background is ImageBrush panelBrush)
            return panelBrush;
        if (control is TemplatedControl templated && templated.Background is ImageBrush templatedBrush)
            return templatedBrush;
        return null;
    }

    /// <summary>
    /// Applies hardcoded draw mode to a single control if it has a mapping.
    /// Used for ExtraControls created after the initial hardcoded pass.
    /// </summary>
    private static void ApplyHardcodedDrawMode(Control control)
    {
        if (!string.IsNullOrEmpty(control.Name)
            && HardcodedDrawModes.TryGetValue(control.Name, out var drawMode))
        {
            var brush = GetImageBrush(control);
            if (brush != null)
            {
                var (stretch, tileMode) = GetDrawModeSettings(drawMode);
                brush.Stretch = stretch;
                brush.TileMode = tileMode;
            }
        }
    }

    /// <summary>
    /// Applies hardcoded draw modes to controls that have Background set.
    /// Called before INI properties are applied, so INI DrawMode can override.
    /// </summary>
    private static void ApplyHardcodedDrawModes(Control root)
    {
        ApplyHardcodedDrawModesRecursive(root);
    }

    private static void ApplyHardcodedDrawModesRecursive(Control parent)
    {
        foreach (var child in GetChildren(parent))
        {
            if (child is Control control)
            {
                if (!string.IsNullOrEmpty(control.Name)
                    && HardcodedDrawModes.TryGetValue(control.Name, out var drawMode))
                {
                    var brush = GetImageBrush(control);
                    if (brush != null)
                    {
                        var (stretch, tileMode) = GetDrawModeSettings(drawMode);
                        brush.Stretch = stretch;
                        brush.TileMode = tileMode;
                    }
                }

                ApplyHardcodedDrawModesRecursive(control);
            }
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

            // Determine stretch mode from hardcoded mapping or default to Fill
            string drawMode = HardcodedDrawModes.TryGetValue(control.Name, out var mode) ? mode : "stretched";
            var (stretch, tileMode) = GetDrawModeSettings(drawMode);

            var brush = new ImageBrush
            {
                Source = bitmap,
                Stretch = stretch,
                TileMode = tileMode,
            };

            if (control is Window window)
                window.Background = brush;
            else if (control is Border border)
            {
                border.Background = brush;
                // Auto-size from texture if no explicit size set
                if (double.IsNaN(border.Width) && double.IsNaN(border.Height))
                {
                    border.Width = bitmap.PixelSize.Width;
                    border.Height = bitmap.PixelSize.Height;
                }
            }
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
    /// Walks all descendant controls and auto-loads standard textures:
    /// - Buttons: {width}pxbtn.png / {width}pxbtn_c.png (XNAClientButton)
    /// - CheckBoxes: checkBoxClear.png / checkBoxChecked.png (XNAClientCheckBox)
    /// MainWindow uses Viewbox(Stretch=Uniform) so Avalonia Width == XNAUI logical Width.
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
            // Skip CheckBox - it has its own textures (checkBoxClear/Checked.png)
            if (child is CheckBox checkBox)
            {
                ApplyCheckBoxTextures(checkBox);
            }
            else if (child is Button button
                && button.Content != null
                && button.Background is not ImageBrush)
            {
                // Match AXAML Width to XNAUI standard button widths
                int[] standardWidths = { 75, 92, 97, 110, 121, 133, 142, 147, 160 };

                int w = !double.IsNaN(button.Width) && button.Width > 0
                    ? standardWidths.OrderBy(sw => Math.Abs(sw - (int)button.Width)).First()
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

                    // Apply FontIndex 1 (bold) by default for standard buttons,
                    // matching XNAClientButton which sets FontIndex = 1 in constructor.
                    var (fontSize, fontWeight) = GetFontConfig(1);
                    button.FontSize = fontSize;
                    button.FontWeight = fontWeight;

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

                    // Text color: ButtonTextColor=AltUIColor (idle) → ButtonHoverColor (hover)
                    // Matches XNA: settings.ButtonTextColor = settings.AltColor (AltUIColor)
                    var buttonTextColor = ParseColorFromConfig("AltUIColor")
                        ?? Color.Parse("#FFFFFF");
                    var buttonHoverColor = ParseColorFromConfig("ButtonHoverColor")
                        ?? Color.Parse("#FCFCFC");
                    button.Foreground = new SolidColorBrush(buttonTextColor);
                    var hoverForeground = new SolidColorBrush(buttonHoverColor);
                    button.PointerEntered += (_, _) => button.Foreground = hoverForeground;
                    button.PointerExited += (_, _) =>
                        button.Foreground = new SolidColorBrush(buttonTextColor);
                }
            }

            ApplyStandardButtonTexturesRecursive(child);
        }
    }

    /// <summary>
    /// Applies checkbox textures matching XNAClientCheckBox behavior:
    /// checkBoxClear.png (unchecked) and checkBoxChecked.png (checked).
    /// Uses a style to override the checkbox indicator with custom images.
    /// Text color: IdleColor=UILabelColor, HighlightColor=AltUIColor on hover.
    /// </summary>
    private static void ApplyCheckBoxTextures(CheckBox checkBox)
    {
        string? clearFile = FindTextureFileStatic("checkBoxClear.png");
        string? checkedFile = FindTextureFileStatic("checkBoxChecked.png");

        if (clearFile == null)
            return;

        var clearBitmap = new Bitmap(clearFile);
        var checkedBitmap = checkedFile != null ? new Bitmap(checkedFile) : clearBitmap;

        var clearBrush = new ImageBrush
        {
            Source = clearBitmap,
            Stretch = Stretch.Fill,
            TileMode = TileMode.None
        };
        var checkedBrush = new ImageBrush
        {
            Source = checkedBitmap,
            Stretch = Stretch.Fill,
            TileMode = TileMode.None
        };

        int imgW = clearBitmap.PixelSize.Width;
        int imgH = clearBitmap.PixelSize.Height;

        // Override the checkbox template entirely to use custom images
        // instead of Avalonia's default checkmark glyph
        var clearBorder = new Border
        {
            Width = imgW,
            Height = imgH,
            Background = clearBrush,
        };
        var checkedBorder = new Border
        {
            Width = imgW,
            Height = imgH,
            Background = checkedBrush,
            IsVisible = checkBox.IsChecked == true,
        };

        // Toggle checked image when IsChecked changes
        checkBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == ToggleButton.IsCheckedProperty)
                checkedBorder.IsVisible = checkBox.IsChecked == true;
        };

        var imageGrid = new Grid
        {
            Width = imgW,
            Height = imgH,
            Children = { clearBorder, checkedBorder },
        };

        // Build new content: image + original text
        var originalContent = checkBox.Content;
        var textBlock = new Avalonia.Controls.TextBlock
        {
            Text = originalContent?.ToString() ?? "",
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };

        // Override the template to show our custom images
        checkBox.Template = new FuncControlTemplate<CheckBox>((cb, _) =>
            new Avalonia.Controls.StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 5,
                Children =
                {
                    imageGrid,
                    textBlock
                }
            });

        // Text color: IdleColor (UILabelColor) → HighlightColor (AltUIColor) on hover
        var idleColor = ParseColorFromConfig("UILabelColor") ?? Color.Parse("#C4C4C4");
        var highlightColor = ParseColorFromConfig("AltUIColor") ?? Color.Parse("#FFFFFF");
        checkBox.Foreground = new SolidColorBrush(idleColor);
        checkBox.PointerEntered += (_, _) => checkBox.Foreground = new SolidColorBrush(highlightColor);
        checkBox.PointerExited += (_, _) => checkBox.Foreground = new SolidColorBrush(idleColor);
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

    /// <summary>
    /// Reads theme colors from DTACnCNetClient.ini and sets them as resources
    /// on both the control's resource dictionary and Application.Current.Resources.
    /// AXAML files can reference these via {DynamicResource XnaTextBrush}, etc.
    /// Matches UISettings color mapping in GameClass.cs.
    /// </summary>
    private static void ApplyThemeColors(Control control)
    {
        // Set on control's resources (for local references)
        SetThemeColorsOnDictionary(control.Resources);

        // Also set on Application resources (for App.axaml styles)
        if (Application.Current != null)
            SetThemeColorsOnDictionary(Application.Current.Resources);
    }

    private static void SetThemeColorsOnDictionary(IResourceDictionary resources)
    {
        // UILabelColor → default text color (XnaTextBrush)
        var textColor = ParseColorFromConfig("UILabelColor") ?? Color.Parse("#C4C4C4");
        resources["XnaTextBrush"] = new SolidColorBrush(textColor);

        // AltUIColor → interactive elements, button text (XnaAltBrush)
        var altColor = ParseColorFromConfig("AltUIColor") ?? Color.Parse("#FFFFFF");
        resources["XnaAltBrush"] = new SolidColorBrush(altColor);

        // ButtonHoverColor → button text on hover (XnaButtonHoverBrush)
        var buttonHoverColor = ParseColorFromConfig("ButtonHoverColor") ?? Color.Parse("#FCFCFC");
        resources["XnaButtonHoverBrush"] = new SolidColorBrush(buttonHoverColor);

        // AltUIBackgroundColor → panel/control backgrounds (XnaPanelBackgroundBrush)
        var bgColor = ParseColorFromConfig("AltUIBackgroundColor") ?? Color.Parse("#000000");
        resources["XnaPanelBackgroundBrush"] = new SolidColorBrush(bgColor);

        // PanelBorderColor → panel borders (XnaPanelBorderBrush)
        var borderColor = ParseColorFromConfig("PanelBorderColor") ?? Color.Parse("#C4C4C4");
        resources["XnaPanelBorderBrush"] = new SolidColorBrush(borderColor);

        // ListBoxFocusColor → list selection highlight (XnaFocusBrush)
        var focusColor = ParseColorFromConfig("ListBoxFocusColor") ?? Color.Parse("#404040");
        resources["XnaFocusBrush"] = new SolidColorBrush(focusColor);

        // DisabledButtonColor → disabled items (XnaDisabledBrush)
        var disabledColor = ParseColorFromConfig("DisabledButtonColor") ?? Color.Parse("#808080");
        resources["XnaDisabledBrush"] = new SolidColorBrush(disabledColor);

        // Subtle/hint text color (XnaSubtleTextBrush)
        var hintColor = ParseColorFromConfig("HintTextColor") ?? Color.Parse("#808080");
        resources["XnaSubtleTextBrush"] = new SolidColorBrush(hintColor);

        // Scrollbar textures (XNAScrollBar)
        LoadTextureResource(resources, "XnaScrollBarBackground", "sbBackground.png");
        LoadTextureResource(resources, "XnaScrollBarUpArrow", "sbUpArrow.png");
        LoadTextureResource(resources, "XnaScrollBarDownArrow", "sbDownArrow.png");
        LoadTextureResource(resources, "XnaScrollBarThumbTop", "sbThumbTop.png");
        LoadTextureResource(resources, "XnaScrollBarThumbMiddle", "sbMiddle.png");
        LoadTextureResource(resources, "XnaScrollBarThumbBottom", "sbThumbBottom.png");

        // ComboBox arrow textures (XNADropDown)
        LoadTextureResource(resources, "XnaComboBoxArrow", "comboBoxArrow.png");
        LoadTextureResource(resources, "XnaComboBoxArrowOpen", "openedComboBoxArrow.png");

        // Trackbar/Slider button texture (XNATrackbar)
        LoadTextureResource(resources, "XnaTrackbarButton", "trackbarButton.png");
    }

    private static void LoadTextureResource(IResourceDictionary resources, string key, string texturePath)
    {
        string? file = FindTextureFileStatic(texturePath);
        if (file != null)
            resources[key] = new Bitmap(file);
    }

    /// <summary>
    /// Applies INI theme colors to ComboBox and TextBox controls, matching
    /// XNADropDown/XNATextBox drawing behavior.
    /// </summary>
    private static void ApplyInputControlStyles(Control root)
    {
        ApplyInputControlStylesRecursive(root);
    }

    private static void ApplyInputControlStylesRecursive(Control parent)
    {
        IEnumerable<Control> children = GetChildren(parent);
        foreach (var child in children)
        {
            if (child is ComboBox comboBox)
            {
                ApplyComboBoxStyle(comboBox);
            }
            else if (child is TextBox textBox)
            {
                ApplyTextBoxStyle(textBox);
            }

            ApplyInputControlStylesRecursive(child);
        }
    }

    /// <summary>
    /// Styles a ComboBox to match XNADropDown: BackColor, BorderColor, TextColor from INI.
    /// XNADropDown uses: BackColor=AltUIBackgroundColor, BorderColor=PanelBorderColor,
    /// TextColor=AltUIColor, FocusColor=ListBoxFocusColor.
    /// </summary>
    private static void ApplyComboBoxStyle(ComboBox comboBox)
    {
        var bgColor = ParseColorFromConfig("AltUIBackgroundColor") ?? Color.Parse("#000000");
        var borderColor = ParseColorFromConfig("PanelBorderColor") ?? Color.Parse("#C4C4C4");
        var textColor = ParseColorFromConfig("AltUIColor") ?? Color.Parse("#FFFFFF");

        comboBox.Background = new SolidColorBrush(bgColor);
        comboBox.BorderBrush = new SolidColorBrush(borderColor);
        comboBox.Foreground = new SolidColorBrush(textColor);
    }

    /// <summary>
    /// Styles a TextBox to match XNATextBox: BackColor, BorderColor, TextColor from INI.
    /// XNATextBox uses: BackColor=AltUIBackgroundColor, IdleBorderColor=PanelBorderColor,
    /// ActiveBorderColor=AltUIColor, TextColor=AltUIColor.
    /// </summary>
    private static void ApplyTextBoxStyle(TextBox textBox)
    {
        var bgColor = ParseColorFromConfig("AltUIBackgroundColor") ?? Color.Parse("#000000");
        var borderColor = ParseColorFromConfig("PanelBorderColor") ?? Color.Parse("#C4C4C4");
        var textColor = ParseColorFromConfig("AltUIColor") ?? Color.Parse("#FFFFFF");

        textBox.Background = new SolidColorBrush(bgColor);
        textBox.BorderBrush = new SolidColorBrush(borderColor);
        textBox.Foreground = new SolidColorBrush(textColor);
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
        var brush = GetImageBrush(control);
        if (brush != null)
        {
            var (stretch, tileMode) = GetDrawModeSettings(drawMode);
            brush.Stretch = stretch;
            brush.TileMode = tileMode;
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
