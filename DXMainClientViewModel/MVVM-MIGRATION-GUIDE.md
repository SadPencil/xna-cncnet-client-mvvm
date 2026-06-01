# MVVM Migration Pitfalls and Lessons Learned

Based on migrating `CampaignSelector.cs` (949 lines) to `CampaignSelectorViewModel.cs`.

## Process

1. **Read the entire source file first.** Do not skim. Understand every method, field, and control interaction before writing a single line.
2. **Copy the entire file into DXMainClientViewModel, then subtract.** Do not write from scratch. Do not "interpret" the code. Copy it, then replace XNA types with MVVM equivalents line by line.
3. **One file at a time.** Read one class, migrate one class, commit one class.

## What the View Is

The View is a dumb rendering layer. It has **zero logic**. It cannot call methods on the ViewModel. It cannot decide to show/hide things based on game state. It only:
- Binds to observable properties (text, visibility, enabled state, list data)
- Invokes commands on user gestures (button clicks, list selection changes)

If you catch yourself writing "View should call..." or "Handled by View when..." — stop. That belongs in the ViewModel.

## Pitfall 1: Inventing Properties That Don't Exist in the Original

**Wrong:** Adding `SelectedCampaignName`, `SelectedCampaignDescription`, `IsMissionPreviewPanelVisible` to the interface because they "seem useful."

**Right:** Only expose what the ViewModel actually computes. The original had `tbMissionDescription.Text = mission.GUIDescription` — so the ViewModel exposes `MissionDescriptionText`. The original had no `SelectedCampaignName` property — don't invent one.

**Rule:** If the original code doesn't have it, the ViewModel doesn't need it. If the original sets a UI control's value, the ViewModel exposes that value as an observable property with the same semantic meaning.

## Pitfall 2: Losing Rich Data During Translation

**Wrong:** Replacing `List<XNAListBoxItem>` (which has `TextColor`, `IsHeader`, `Selectable`, `Texture`) with `IReadOnlyList<string>` (just names).

**Right:** Create a `CampaignListItem` data model that preserves all the data the original code computed: `Text`, `TextColorKind`, `IsHeader`, `IsSelectable`, `IconPath`. The View uses this data to render — but the data itself is business logic.

**Rule:** When the original code builds complex objects for UI display, create a data model class in the ViewModel that carries all the same information. Don't collapse it to strings.

## Pitfall 3: Making the Interface a Method Abstraction

**Wrong:** Adding `void LoadMissionsWithFilter(...)` to `ICampaignSelectorViewModel` because the original had it as a public method.

**Right:** The interface is for View-ViewModel communication. The View cannot call methods. `LoadMissionsWithFilter` is called internally (from `ReadMissionList` and potentially from `CampaignTagSelector` — which is a sibling ViewModel concern, not a View concern). It stays as a public method on the concrete class but is NOT on the interface.

**Rule:** The `IXxxViewModel` interface only exposes: observable properties, commands, and read-only data collections. No methods. Ever. If you need a method for inter-ViewModel communication, use a service or a different interface.

## Pitfall 4: Making Both Branches Do the Same Thing

**Wrong:**
```csharp
if (ClientConfiguration.Instance.ReturnToMainMenuOnMissionLaunch)
    IsControlsEnabled = false;  // original: Disable() — hides the window
else
    IsControlsEnabled = false;  // original: ToggleControls(false) — just disables controls
```

**Right:** The original distinguished between hiding the window (`Disable()`) and disabling controls (`ToggleControls(false)`). The ViewModel needs `IsVisible = false` for the first case and `IsControlsEnabled = false` for the second.

**Rule:** When the original code has two distinct UI actions (hide vs disable, show vs enable), the ViewModel must have two distinct observable properties. Don't collapse different behaviors into one property.

## Pitfall 5: Dropping Parameters from Service Calls

**Wrong:** `discordHandler.SetCampaignPresence(mission.UntranslatedGUIName, difficultyName)` — missing `mission.IconPath` and `true` (resetTimer).

**Right:** `discordHandler.SetCampaignPresence(mission.UntranslatedGUIName, difficultyName, mission.IconPath, true)` — matches the original `discordHandler.UpdatePresence(mission.UntranslatedGUIName, difficultyName, mission.IconPath, true)`.

**Rule:** When creating a service interface to wrap an existing class, preserve the exact same parameters. Don't simplify. Don't "clean up." The parameters exist for a reason.

## Pitfall 6: Using the Wrong Type for Nullable Fields

**Wrong:** `private IniFile gameOptionsIni;` — non-nullable, but the original didn't mark it nullable either.

This is acceptable because the constructor always initializes it. But be aware: if the original code has a field that could be null (like `missionToLaunch`), make it nullable (`Mission? missionToLaunch`).

**Rule:** Match the original nullability semantics. If the original assigns null to a field, make it nullable. If the original always initializes before use, non-nullable is fine.

## Pitfall 7: Renaming Things Without Necessity

**Wrong:** Renaming `DifficultyNames` to `DifficultyNamesArray` just because CommunityToolkit.Mvvm's source generator conflicts with the field name.

Actually, this renaming was necessary — the source generator creates a property called `DifficultyNames` from the field `difficultyNames`, which would conflict with the static field. But document WHY you renamed it.

**Rule:** If you must rename something, add a comment explaining why. Don't silently rename.

## Pitfall 8: Forgetting the `Return` Command Body

**Wrong:** Leaving the `Return` command completely empty and not thinking about what it means.

**Right:** The original calls `campaignTagSelector.NoFadeSwitch()` — which is a navigation action. In MVVM, navigation is a View concern. The command body is empty, but the View observes the command execution and performs the navigation. This is correct — but you must understand WHY it's empty.

**Rule:** Empty command bodies are acceptable only when the action is purely a View concern (navigation, animation, sound effect). Document why it's empty.

## Pitfall 9: Not Checking for Missing Logic After "Completing" the Migration

**Wrong:** Declaring migration complete without line-by-line verification.

**Right:** Read the original file again, method by method. For each method, verify:
1. Is the logic present in the ViewModel? (business logic → yes, UI rendering → no)
2. Are all parameters preserved? (service calls, method signatures)
3. Are all branches preserved? (if/else, switch cases)
4. Are all edge cases preserved? (null checks, bounds checks)

**Rule:** After writing the ViewModel, re-read the original file from top to bottom. Compare every method. Don't trust your memory.

## Quick Reference: What Goes Where

| Original Code | Destination | Why |
|---|---|---|
| `XNAWindow` base class | `ObservableObject` | MVVM base |
| `XNAListBox`, `XNAClientButton`, etc. | Observable properties | View binds to them |
| `btnLaunch.AllowClick = false` | `CanLaunchCampaign = false` | Semantic property |
| `lbCampaignList.SelectedIndex` | `SelectedCampaignIndex` | Two-way binding |
| `tbMissionDescription.Text` | `MissionDescriptionText` | One-way binding |
| `Disable()` | `IsVisible = false` | View hides itself |
| `ToggleControls(false)` | `IsControlsEnabled = false` | View disables controls |
| `cheaterWindow.Enable()` | `IsCheaterWindowVisible = true` | View shows cheater window |
| `btnLaunch.LeftClick += handler` | `[RelayCommand]` | Command pattern |
| `SelectedIndexChanged += handler` | `partial void OnXxxChanged()` | CommunityToolkit.Mvvm |
| `DiscordHandler` | `IDiscordHandlerService` | Interface abstraction |
| `Updater.IsFileNonexistantOrOriginal` | `IFileIntegrityService` | Interface abstraction |
| `GameProcessLogic` | `ICampaignGameProcessService` | Interface abstraction |
| `CampaignCheckBox` | `ICampaignCheckBoxOption` | Interface abstraction |
| `CampaignDropDown` | `ICampaignDropDownOption` | Interface abstraction |
| `XNAListBoxItem` (TextColor, IsHeader, etc.) | `CampaignListItem` data model | Preserve rich data |
| `AssetLoader.LoadTexture(...)` | Nothing (View concern) | Don't migrate |
| `CreateLetterboxedTexture(...)` | Nothing (View concern) | Don't migrate |
| `Draw(GameTime)` | Nothing (View concern) | Don't migrate |
| `AddChild(...)` | Nothing (View concern) | Don't migrate |
| `CenterOnParent()` | Nothing (View concern) | Don't migrate |
| `Initialize()` UI creation | Nothing (View concern) | Don't migrate |
| `Initialize()` business logic | Constructor | Migrate to constructor |
