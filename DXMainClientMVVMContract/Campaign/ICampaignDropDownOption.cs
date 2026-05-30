using Rampastring.Tools;

namespace DXMainClientViewModel.Campaign;

/// <summary>
/// Abstracts a campaign dropdown option's business logic.
/// The View creates the actual UI control from INI and registers an implementation here.
/// </summary>
public interface ICampaignDropDownOption
{
    string Name { get; }
    int SelectedIndex { get; set; }
    int ItemCount { get; }
    void ApplySpawnIniCode(IniFile spawnIni);
    void ApplyMapCode(IniFile mapIni, object? gameMode);
}
