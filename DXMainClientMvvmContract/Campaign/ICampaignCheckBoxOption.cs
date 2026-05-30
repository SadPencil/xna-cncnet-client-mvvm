using Rampastring.Tools;

namespace DXMainClientMvvmContract.Campaign;

/// <summary>
/// Abstracts a campaign checkbox option's business logic.
/// The View creates the actual UI control from INI and registers an implementation here.
/// </summary>
public interface ICampaignCheckBoxOption
{
    string Name { get; }
    bool Checked { get; set; }
    bool ResetToDefaultOnGameExit { get; }
    void ResetToDefault();
    void ApplySpawnIniCode(IniFile spawnIni);
    void ApplyMapCode(IniFile mapIni, object? gameMode);
}
