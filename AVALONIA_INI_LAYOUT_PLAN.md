# Avalonia UI with INI-Based Layout Support: Implementation Plan

## Problem Statement

The XNA CnCNet Client uses an INI-driven layout system where modders customize GUI layout via `.ini` files. The refactoring goal is to replace the XNAUI-based View layer with Avalonia UI while preserving INI-based layout customization so existing mods continue to work.

**Core tension**: Avalonia uses `.axaml` for layout (compiled, declarative XML). The old system uses `.ini` files (runtime, key-value). We need both to coexist.

## Architecture Overview

### Recommended Approach: AXAML Defaults + INI Runtime Overlay

```
┌─────────────────────────────────────────────┐
│              Avalonia Window (.axaml)         │
│  - Defines default layout structure           │
│  - All controls have x:Name matching INI      │
│  - Binds to ViewModel via {Binding}           │
└──────────────────┬──────────────────────────┘
                   │ Loaded at startup
                   ▼
┌─────────────────────────────────────────────┐
│           INI Layout Overlay Service          │
│  - Reads INI file for the window              │
│  - Applies position/size/visibility overrides │
│  - Evaluates expressions ($X, $Y, etc.)       │
│  - Creates ExtraControls from $CC directives  │
└──────────────────┬──────────────────────────┘
                   │ Applied after AXAML load
                   ▼
┌─────────────────────────────────────────────┐
│         Final Rendered Layout                 │
│  - AXAML defaults + INI overrides merged      │
│  - Identical to what XNAUI would produce      │
└─────────────────────────────────────────────┘
```

**Why this approach**:
1. AXAML provides the default layout (developer-friendly, compiled bindings)
2. INI overrides preserve modder workflow (runtime customization)
3. MVVM is maintained: View observes ViewModel, INI only affects layout
4. No business logic leaks into View - INI overlay only touches layout properties

## Detailed Design

### 1. Control Name Mapping (Critical)

Every Avalonia control must have an `x:Name` matching the INI section name. This is the same contract the old system uses - `XNAControl.Name` matches the INI section `[Name]`.

```xml
<!-- CampaignSelector.axaml -->
<Window x:Name="CampaignSelector" ...>
  <Canvas>
    <ListBox x:Name="lbCampaignList" ... />
    <TextBlock x:Name="lblMissionDescriptionHeader" ... />
    <TextBlock x:Name="tbMissionDescription" ... />
    <Button x:Name="btnLaunch" ... />
    <Button x:Name="btnCancel" ... />
  </Canvas>
</Window>
```

INI file:
```ini
[CampaignSelector]
Size=672,600

[lbCampaignList]
Size=322,554

[btnLaunch]
Width=147
DistanceFromRightBorder=179
```

### 2. XNAUI-to-Avalonia Property Mapping

| INI Property | XNAUI Property | Avalonia Equivalent |
|---|---|---|
| `X` | `control.X` | `Canvas.Left` (attached) |
| `Y` | `control.Y` | `Canvas.Top` (attached) |
| `Location=X,Y` | `control.X`, `control.Y` | `Canvas.Left`, `Canvas.Top` |
| `Width` | `control.Width` | `Width` |
| `Height` | `control.Height` | `Height` |
| `Size=W,H` | `Width`, `Height` | `Width`, `Height` |
| `Text` | `control.Text` | `Content` (Button), `Text` (TextBlock/TextBox) |
| `Visible` | `control.Visible` | `IsVisible` |
| `Enabled` | `control.Enabled` | `IsEnabled` |
| `DistanceFromRightBorder` | computed | Computed from parent width |
| `DistanceFromBottomBorder` | computed | Computed from parent height |
| `FillWidth` | computed | Computed: `parent.Width - X - value` |
| `FillHeight` | computed | Computed: `parent.Height - Y - value` |
| `RemapColor` | color tint | `Opacity` or custom |
| `BackgroundTexture` | texture load | `Background` with image |
| `IdleTexture` (button) | button texture | `Background` with image |
| `HoverTexture` (button) | hover texture | Pseudo-class `:pointerover` |
| `TextColor` | label color | `Foreground` |
| `FontIndex` | font index | Mapped to font family/size |
| `DrawBorders` | border drawing | `Border` element |
| `Padding` | padding | `Padding` |
| `DrawMode` | Stretched/Centered/Tiled | `Stretch` on Image |

### 3. INI Layout Overlay Service

Create a new service in `AvMainClientView` that applies INI overrides to Avalonia controls:

```csharp
// AvMainClientView/Services/IIniLayoutOverlayService.cs
public interface IIniLayoutOverlayService
{
    void ApplyLayout(Window window, string windowName);
    void ApplyLayout(Control control, string controlName, CCIniFile iniFile);
}
```

**Implementation responsibilities**:
1. Locate the INI file (theme path -> base path -> GenericWindow.ini fallback)
2. Read the window's section, apply properties
3. For each child control with `x:Name`, look up its INI section and apply overrides
4. Handle `DistanceFromRightBorder`/`DistanceFromBottomBorder` by computing absolute positions
5. Handle `FillWidth`/`FillHeight` by computing sizes
6. Process `ExtraControls` section to create additional controls
7. Process `$CC` directives for `INItializableWindow`-style windows

### 4. Expression Parser Port

The `Parser` class (`ClientGUI/Parser.cs`) needs to be ported to work with Avalonia controls. The expression language is the same, but the control property access changes:

```
getX(controlName)  -> Canvas.GetLeft(control) or control.Margin.Left
getY(controlName)  -> Canvas.GetTop(control) or control.Margin.Top
getWidth(control)  -> control.Width or control.Bounds.Width
getHeight(control) -> control.Height or control.Bounds.Height
getBottom(control) -> Canvas.GetTop(control) + control.Height
getRight(control)  -> Canvas.GetLeft(control) + control.Width
```

**Decision**: Port `Parser.cs` to `AvMainClientView` as `AvaloniaParser.cs`, operating on `Control` instead of `XNAControl`.

### 5. Avalonia Control Type Mapping

| XNAUI Type | Avalonia Type | Notes |
|---|---|---|
| `XNAControl` | `Panel` | Base container |
| `XNAPanel` | `Border` + `Panel` | With background/border |
| `XNAExtraPanel` | `Border` | Decorative panels |
| `XNALabel` | `TextBlock` | Text display |
| `XNAButton` | `Button` | With image backgrounds |
| `XNAClientButton` | `Button` | With tooltip |
| `XNACheckBox` | `CheckBox` | Standard checkbox |
| `XNAClientCheckBox` | `CheckBox` | With tooltip |
| `XNADropDown` | `ComboBox` | Dropdown selection |
| `XNAClientDropDown` | `ComboBox` | With tooltip |
| `XNATextBox` | `TextBox` | Text input |
| `XNASuggestionTextBox` | `TextBox` | With watermark |
| `XNAListBox` | `ListBox` | List display |
| `XNAMultiColumnListBox` | `DataGrid` or custom | Multi-column list |
| `XNATabControl` | `TabControl` | Tabbed interface |
| `XNATrackbar` | `Slider` | Slider control |
| `XNALinkButton` | `Button` + `Process.Start` | URL button |
| `GameLaunchButton` | Custom `Button` | Game-specific |
| `GameLobbyCheckBox` | `CheckBox` | With lobby binding |
| `GameLobbyDropDown` | `ComboBox` | With lobby binding |
| `GameSessionCheckBox` | `CheckBox` | With session binding |
| `GameSessionDropDown` | `ComboBox` | With session binding |
| `MapPreviewBox` | Custom `Control` | Map preview |

### 6. Window Decoration System

The old system adds decorative panels (glow effects, bars) via `ExtraControls` and `$ExtraControls`. These need to be recreated in AXAML:

```xml
<!-- GenericWindow template as UserControl or Styles -->
<Style Selector="Window.generic_window">
  <Setter Property="Template">
    <ControlTemplate>
      <Panel>
        <!-- Window chrome bars -->
        <Image x:Name="bar_ul" Source="bar_ul.png" Canvas.Left="-24" Canvas.Top="0" />
        <Image x:Name="bar_ur" Source="bar_ur.png" />
        <!-- ... glow effects ... -->
        <ContentPresenter /> <!-- Window content -->
      </Panel>
    </ControlTemplate>
  </Setter>
</Style>
```

**Alternative**: Create a `GenericWindowChrome` UserControl that encapsulates all the decoration panels, applied to windows that need it.

### 7. Two INI System Support

**System A (XNAWindow-based)**: `CampaignSelector`, `MainMenu`, `OptionsWindow`, etc.
- AXAML defines default layout with named controls
- INI overlay applies overrides after AXAML load
- `ExtraControls` section creates additional controls

**System B (INItializableWindow-based)**: `GameLobbyBase`, `SkirmishLobby`, `CnCNetLobby`, etc.
- AXAML defines the window shell and static controls
- `$CC` directives in INI create dynamic controls (game options, etc.)
- Expression parser evaluates `$X`, `$Y`, `$Width`, `$Height`
- These windows have MORE dynamic content - AXAML provides the skeleton, INI fills it

**Key insight**: System B windows (`GameLobbyBase` etc.) have controls created entirely from INI. The AXAML for these windows will be minimal - just the window container. The INI overlay service will create and position most controls dynamically.

### 8. Texture/Image Handling

The old system loads textures via `AssetLoader.LoadTexture("path.png")`. In Avalonia:

```csharp
// Convert XNA texture path to Avalonia Bitmap
public static class AssetConverter
{
    public static IImage LoadAvaloniaImage(string texturePath)
    {
        // Search theme paths, then base paths
        // Return Avalonia.Media.Imaging.Bitmap
    }
}
```

INI `BackgroundTexture=MainMenu/dbak.png` maps to:
```xml
<Border Background="{DynamicResource MainMenu/dbak.png}" />
```
Or applied at runtime via the overlay service.

### 9. Font System

The old system uses `FontIndex` (0, 1, 2, etc.) mapped to loaded fonts. In Avalonia:

```csharp
// Map font indices to Avalonia font families/sizes
public static class FontMapper
{
    public static FontFamily GetFont(int index) { ... }
    public static double GetFontSize(int index) { ... }
}
```

### 10. Color System

INI colors are `R,G,B` or `R,G,B,A` (0-255). Avalonia uses hex or `Color`:

```csharp
public static Color ParseIniColor(string value)
{
    // "255,255,255" -> Color.FromRgb(255, 255, 255)
    // "255,255,255,128" -> Color.FromArgb(128, 255, 255, 255)
}
```

## Implementation Phases

### Phase 1: Foundation (Infrastructure)

1. **AvaloniaParser** - Port `Parser.cs` to work with Avalonia `Control`
2. **IniLayoutOverlayService** - Core service that reads INI and applies to Avalonia controls
3. **AssetConverter** - Load XNA textures as Avalonia bitmaps
4. **FontMapper** - Map font indices to Avalonia fonts
5. **ColorParser** - Parse INI color strings to Avalonia `Color`
6. **ControlFactory** - Create Avalonia controls from INI type names (replaces `ClientGUICreator`)

### Phase 2: GenericWindow Template

7. **GenericWindow.axaml** - Window chrome (glow bars, decorations) as reusable template
8. **GenericWindow INI integration** - Apply `GenericWindow.ini` overrides to the template

### Phase 3: Simple Windows (System A)

9. **CheaterWindow.axaml** - Already done, add INI overlay
10. **CampaignSelector.axaml** - Already done, add INI overlay
11. **LoadingScreen.axaml**
12. **MainMenu.axaml**
13. **OptionsWindow.axaml** (with option panels)
14. **UpdateWindow.axaml**, **UpdateQueryWindow.axaml**, **ManualUpdateQueryWindow.axaml**
15. **PrivacyNotification.axaml**, **StatisticsWindow.axaml**, **ExtrasWindow.axaml**
16. **GameInProgressWindow.axaml**, **GameLoadingWindow.axaml**

### Phase 4: Complex Windows (System B)

17. **GameLobbyBase.axaml** - Skeleton with dynamic control creation
18. **SkirmishLobby.axaml** - Extends GameLobbyBase
19. **MultiplayerGameLobby.axaml** - Extends GameLobbyBase
20. **CnCNetGameLobby.axaml**, **LANGameLobby.axaml**
21. **CnCNetLobby.axaml**, **LANLobby.axaml**

### Phase 5: Specialized Controls

22. **MapPreviewBox.axaml** - Custom map preview control
23. **PlayerListBox.axaml** - Player list with status icons
24. **GameListBox.axaml** - Game list with filtering
25. **ChatListBox.axaml** - Chat message list
26. **GameLaunchButton.axaml** - Custom launch button

### Phase 6: Dialogs and Panels

27. **CnCNetLoginWindow.axaml**, **PasswordRequestWindow.axaml**
28. **GameCreationWindow.axaml**, **TunnelSelectionWindow.axaml**
29. **PrivateMessagingWindow.axaml**
30. **GameLobbySettingsWindow.axaml**

## INI Overlay Implementation Details

### Property Application Order

1. AXAML defaults (compiled into the window)
2. INI section properties (override AXAML defaults)
3. Expression-evaluated properties ($X, $Y, $Width, $Height)
4. ExtraControls creation
5. Post-processing (FillWidth, FillHeight, DistanceFromRightBorder, etc.)

### Handling DistanceFromRightBorder / DistanceFromBottomBorder

These are computed properties that require knowing the parent's size:

```csharp
private void ApplyDistanceFromBorders(Control control, CCIniFile ini, string section)
{
    int? distRight = ini.GetIntValue(section, "DistanceFromRightBorder", null);
    int? distBottom = ini.GetIntValue(section, "DistanceFromBottomBorder", null);

    var parent = control.GetVisualParent() as Control;
    if (parent == null) return;

    if (distRight.HasValue)
    {
        double x = parent.Bounds.Width - control.Width - distRight.Value;
        Canvas.SetLeft(control, x);
    }

    if (distBottom.HasValue)
    {
        double y = parent.Bounds.Height - control.Height - distBottom.Value;
        Canvas.SetTop(control, y);
    }
}
```

### Handling FillWidth / FillHeight

```csharp
private void ApplyFill(Control control, CCIniFile ini, string section)
{
    int? fillWidth = ini.GetIntValue(section, "FillWidth", null);
    int? fillHeight = ini.GetIntValue(section, "FillHeight", null);

    var parent = control.GetVisualParent() as Control;
    if (parent == null) return;

    if (fillWidth.HasValue)
    {
        double x = Canvas.GetLeft(control);
        control.Width = parent.Bounds.Width - x - fillWidth.Value;
    }

    if (fillHeight.HasValue)
    {
        double y = Canvas.GetTop(control);
        control.Height = parent.Bounds.Height - y - fillHeight.Value;
    }
}
```

### Handling ExtraControls / $CC

```csharp
private void CreateExtraControls(Window window, CCIniFile ini)
{
    var section = ini.GetSection("$ExtraControls") ?? ini.GetSection("ExtraControls");
    if (section == null) return;

    foreach (var kvp in section.Keys)
    {
        string key = kvp.Key;
        string value = kvp.Value;

        // Skip non-$CC keys for $ExtraControls section
        if (section.Name == "$ExtraControls" && !key.StartsWith("$CC"))
            continue;

        string[] parts = value.Split(':');
        if (parts.Length != 2) continue;

        string controlName = parts[0];
        string controlType = parts[1];

        // Check if control already exists (from AXAML)
        if (FindControlByName(window, controlName) != null)
            continue;

        // Create control via factory
        var control = ControlFactory.Create(controlType);
        control.Name = controlName;

        // Apply INI attributes
        ApplyControlAttributes(control, ini, controlName);

        // Add to parent (find parent from INI or default to window's content)
        AddControlToParent(window, control);
    }
}
```

### Handling $LeftClickAction

```csharp
private void ApplyLeftClickAction(Control control, string action)
{
    if (action == "Disable")
    {
        if (control is Button button)
            button.Click += (s, e) => control.IsEnabled = false;
    }
}
```

## Key Challenges and Solutions

### Challenge 1: Avalonia Uses Different Layout Model

**Problem**: XNAUI uses absolute positioning (X, Y). Avalonia prefers panels (StackPanel, Grid, DockPanel).

**Solution**: Use `Canvas` as the root container for INI-overridable windows. Canvas provides absolute positioning via `Canvas.Left` and `Canvas.Top`, matching XNAUI's model. The AXAML defines defaults, INI overrides them.

### Challenge 2: Texture-Based Controls

**Problem**: XNAUI controls use textures (`IdleTexture`, `HoverTexture`, `BackgroundTexture`). Avalonia uses styles and templates.

**Solution**: Create custom Avalonia controls or use styles with `Background` set to loaded bitmaps. For buttons with idle/hover textures, use pseudo-classes:

```xml
<Button x:Name="btnLaunch">
  <Button.Styles>
    <Style Selector="Button">
      <Setter Property="Background">
        <ImageBrush Source="147pxbtn.png" />
      </Setter>
    </Style>
    <Style Selector="Button:pointerover">
      <Setter Property="Background">
        <ImageBrush Source="147pxbtn_c.png" />
      </Setter>
    </Style>
  </Button.Styles>
</Button>
```

### Challenge 3: Expression Parser Dependencies

**Problem**: The Parser references sibling controls by name and reads their current X/Y/Width/Height.

**Solution**: The AvaloniaParser maintains a dictionary of named controls within the window. After AXAML load (and before INI override), register all named controls. The parser queries this dictionary for `getX()`, `getY()`, etc.

### Challenge 4: Dynamic Control Creation ($CC)

**Problem**: Some windows create most of their controls dynamically from INI (e.g., GameLobbyBase creates 30+ checkboxes/dropdowns).

**Solution**: The INI overlay service handles `$CC` directives by:
1. Parsing the `$CC00=controlName:ControlType` format
2. Using `ControlFactory.Create(controlType)` to instantiate Avalonia controls
3. Applying INI attributes to the new control
4. Adding the control to the appropriate parent (Canvas or Panel)

For these windows, the AXAML provides only the window shell. The INI overlay fills in the content.

### Challenge 5: BasedOn / $BaseSection Inheritance

**Problem**: INI files use `BasedOn` for file-level inheritance and `$BaseSection` for section-level inheritance.

**Solution**: Use the existing `CCIniFile` class from `ClientCore` (already migrated to `AvMainClientViewModel`). It handles both inheritance mechanisms. The INI overlay service just needs to load the file and query sections.

### Challenge 6: Localization

**Problem**: The old system uses `TranslationINIParser` to intercept certain keys and apply localization.

**Solution**: The INI overlay service includes a localization step:

```csharp
private string Localize(string controlName, string attributeName, string value)
{
    // Check if translation exists for this control/attribute
    // Return translated value or original
}
```

## File Structure

```
AvMainClientView/
├── Services/
│   ├── IIniLayoutOverlayService.cs      # INI overlay interface
│   ├── IniLayoutOverlayService.cs       # INI overlay implementation
│   ├── AvaloniaParser.cs                # Expression parser for Avalonia
│   ├── AssetConverter.cs                # XNA texture -> Avalonia bitmap
│   ├── FontMapper.cs                    # Font index -> Avalonia font
│   ├── ColorParser.cs                   # INI color -> Avalonia Color
│   └── ControlFactory.cs               # INI type name -> Avalonia control
├── Controls/
│   ├── GenericWindowChrome.cs           # Window decoration template
│   ├── MapPreviewBox.cs                 # Custom map preview
│   ├── GameLaunchButton.cs              # Custom launch button
│   └── ...                              # Other custom controls
├── Campaign/
│   ├── CampaignSelector.axaml           # (exists)
│   ├── CampaignSelector.axaml.cs        # (exists)
│   ├── CheaterWindow.axaml              # (exists)
│   └── CheaterWindow.axaml.cs           # (exists)
├── Generic/
│   ├── MainMenu.axaml
│   ├── TopBar.axaml
│   ├── OptionsWindow.axaml
│   ├── LoadingScreen.axaml
│   └── ...
├── Multiplayer/
│   ├── GameLobby/
│   │   ├── GameLobbyBase.axaml
│   │   ├── SkirmishLobby.axaml
│   │   └── ...
│   ├── CnCNet/
│   │   ├── CnCNetLobby.axaml
│   │   └── ...
│   └── ...
└── App.axaml                            # Avalonia Application entry point
```

## Migration Strategy

### Per-Window Migration (One at a Time)

For each window:

1. **Read the original DXMainClient class** (e.g., `CampaignSelector.cs`)
2. **Read the INI file** (e.g., `CampaignSelector.ini`)
3. **Create AXAML** with named controls matching INI sections
4. **Add INI overlay call** in code-behind or startup
5. **Test** that INI overrides still work
6. **Commit and push**

### Code-Behind Pattern

```csharp
public partial class CampaignSelector : Window, ICampaignSelectorView
{
    private readonly IIniLayoutOverlayService _iniOverlay;

    public CampaignSelector(IIniLayoutOverlayService iniOverlay)
    {
        _iniOverlay = iniOverlay;
        InitializeComponent();
        _iniOverlay.ApplyLayout(this, "CampaignSelector");
    }

    public ICampaignSelectorViewModel? ViewModel
    {
        get => DataContext as ICampaignSelectorViewModel;
        set => DataContext = value;
    }

    void ICampaignSelectorView.Show() => this.Show();
    void ICampaignSelectorView.Hide() => Close();
}
```

## Open Questions

1. **Should we support ALL INI properties or just layout properties?**
   - Recommendation: Start with layout (X, Y, Width, Height, Visible, Enabled, Text)
   - Add texture/color/font support incrementally

2. **How to handle controls created entirely from INI ($CC)?**
   - These windows (GameLobbyBase etc.) need the ControlFactory
   - AXAML provides the shell, INI fills in dynamic controls

3. **Should the Parser be ported or rewritten?**
   - Port the existing Parser.cs logic, adapting to Avalonia's Control type
   - The expression language is simple and well-tested

4. **How to handle window chrome (glow bars, decorations)?**
   - Create a reusable `GenericWindowChrome` UserControl
   - Apply via styles or as a wrapper around window content

5. **Performance: INI reading at every window open?**
   - Cache parsed INI files (CCIniFile already supports this)
   - Apply overrides once at window initialization

## Summary

The recommended approach is **AXAML Defaults + INI Runtime Overlay**:

- AXAML files define the default layout with named controls
- A runtime INI overlay service reads INI files and applies property overrides
- The expression parser is ported to work with Avalonia controls
- Control types are mapped from XNAUI to Avalonia equivalents
- Modders can still customize layout via INI files
- MVVM is preserved: View only observes ViewModel, INI only affects layout

This approach minimizes the gap between old and new systems while maintaining clean MVVM architecture.
