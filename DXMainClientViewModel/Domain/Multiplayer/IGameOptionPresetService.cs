#nullable enable
using System.Collections.Generic;

namespace DXMainClientViewModel;

public interface IGameOptionPresetService
{
    IReadOnlyList<string> PresetNames { get; }

    bool ContainsPreset(string presetName);
    IReadOnlyDictionary<string, string> LoadPreset(string presetName);
    void SavePreset(string presetName, IReadOnlyDictionary<string, string> optionValues);
    void DeletePreset(string presetName);
}
