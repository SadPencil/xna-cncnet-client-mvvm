using DXMainClientMvvmContract.Campaign;

using Rampastring.Tools;

namespace DXMainClientViewModel.Campaign
{
    public class CampaignCheckBoxOption : ICampaignCheckBoxOption
    {
        public string Name { get; set; }
        public bool Checked { get; set; }
        public bool ResetToDefaultOnGameExit { get; set; }

        // TODO: read the old client's code and implement this properly in an MVVM way.
        public void ResetToDefault() { }
        public void ApplyMapCode(IniFile mapIni, string gameMode) { }
        public void ApplySpawnIniCode(IniFile spawnIni) { }
    }
}
