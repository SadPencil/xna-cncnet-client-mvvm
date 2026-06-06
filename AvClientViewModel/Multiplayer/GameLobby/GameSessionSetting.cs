using System;
using System.Collections.Generic;

using AvClientMvvmContract.Domain.Multiplayer;
using AvClientViewModel.Domain.Multiplayer;

using Rampastring.Tools;

namespace AvClientViewModel.Multiplayer.GameLobby;

/// <summary>
/// Determines how a checkbox setting affects scoring/ranking eligibility.
/// </summary>
public enum CheckBoxMapScoringMode
{
    /// <summary>The check box value makes no difference for scoring.</summary>
    Irrelevant = 0,

    /// <summary>Scoring is denied when the check box is checked.</summary>
    DenyWhenChecked = 1,

    /// <summary>Scoring is denied when the check box is unchecked.</summary>
    DenyWhenUnchecked = 2
}

/// <summary>
/// Data model for a game session setting (checkbox or dropdown option).
/// Contains configuration data and implements spawn.ini / map code application logic
/// migrated from the old XNA GameSessionCheckBox and GameSessionDropDown controls.
/// </summary>
public class GameSessionSetting : IGameSessionSetting
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }

    public bool AffectsSpawnIni { get; set; }
    public bool AffectsMapCode { get; set; }

    public bool AllowScoring =>
        !((MapScoringMode == CheckBoxMapScoringMode.DenyWhenChecked && Value != 0)
       || (MapScoringMode == CheckBoxMapScoringMode.DenyWhenUnchecked && Value == 0));

    public bool BroadcastToLobby { get; set; }

    // --- Configuration fields (checkbox) ---
    public CheckBoxMapScoringMode MapScoringMode { get; set; }
    public string SpawnIniOption { get; set; } = string.Empty;
    public bool Reversed { get; set; }
    public string EnabledSpawnIniValue { get; set; } = string.Empty;
    public string DisabledSpawnIniValue { get; set; } = string.Empty;
    public string CustomIniPath { get; set; } = string.Empty;
    public List<int>? DisallowedSideIndices { get; set; }

    // --- Configuration fields (dropdown) ---
    public DropDownDataWriteMode DataWriteMode { get; set; }
    public List<string>? DropDownItemTags { get; set; }
    public string OptionName { get; set; } = string.Empty;

    public IReadOnlyList<string>? DropDownItems => DropDownItemTags;

    public void ApplySpawnIniCode(IniFile spawnIni)
    {
        if (!AffectsSpawnIni)
            return;

        if (string.IsNullOrEmpty(SpawnIniOption))
            return;

        if (DropDownItemTags != null)
        {
            // DropDown mode
            if (Value < 0 || Value >= DropDownItemTags.Count)
                return;

            switch (DataWriteMode)
            {
                case DropDownDataWriteMode.BOOLEAN:
                    spawnIni.SetBooleanValue("Settings", SpawnIniOption, Value > 0);
                    break;
                case DropDownDataWriteMode.INDEX:
                    spawnIni.SetIntValue("Settings", SpawnIniOption, Value);
                    break;
                default:
                    spawnIni.SetStringValue("Settings", SpawnIniOption, DropDownItemTags[Value]);
                    break;
            }
        }
        else
        {
            // CheckBox mode
            string value = DisabledSpawnIniValue;
            if ((Value != 0) != Reversed)
                value = EnabledSpawnIniValue;

            spawnIni.SetStringValue("Settings", SpawnIniOption, value);
        }
    }

    public void ApplyMapCode(IniFile mapIni, IGameMode gameMode)
    {
        if (!AffectsMapCode)
            return;

        // In checkbox mode: only apply if checked and not reversed
        if (DropDownItemTags != null)
        {
            // DropDown mode
            if (Value < 0 || Value >= DropDownItemTags.Count)
                return;
            MapCodeHelper.ApplyMapCode(mapIni, DropDownItemTags[Value], (GameMode)gameMode);
        }
        else
        {
            // CheckBox mode
            if ((Value != 0) == Reversed)
                return;

            if (!string.IsNullOrEmpty(CustomIniPath))
                MapCodeHelper.ApplyMapCode(mapIni, CustomIniPath, (GameMode)gameMode);
        }
    }

    public void ApplyDisallowedSideIndex(bool[] disallowedArray)
    {
        if (DisallowedSideIndices == null || DisallowedSideIndices.Count == 0)
            return;

        if ((Value != 0) != Reversed)
        {
            for (int i = 0; i < DisallowedSideIndices.Count; i++)
                disallowedArray[DisallowedSideIndices[i]] = true;
        }
    }
}

/// <summary>
/// Determines how a dropdown setting's value is written to spawn.ini.
/// </summary>
public enum DropDownDataWriteMode
{
    STRING,
    BOOLEAN,
    INDEX,
    MAPCODE
}
