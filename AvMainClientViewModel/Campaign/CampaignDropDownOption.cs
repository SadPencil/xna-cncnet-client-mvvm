using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AvMainClientMvvmContract.Campaign;

using Rampastring.Tools;

namespace AvMainClientViewModel.Campaign
{
    public class CampaignDropDownOption : ICampaignDropDownOption
    {
        public string Name { get; set; }

        public int SelectedIndex { get; set; }

        public int ItemCount { get; set; }

        // TODO: read the old client's code and implement this properly in an MVVM way.
        public void ApplyMapCode(IniFile mapIni, string gameMode) { }
        public void ApplySpawnIniCode(IniFile spawnIni) { }
    }
}
