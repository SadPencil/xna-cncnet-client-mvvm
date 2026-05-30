using System.Collections.Generic;
using System.ComponentModel;

namespace DXMainClientMvvmContract.Domain;

/// <summary>
/// Read-only view of a mission definition.
/// </summary>
public interface IMission : INotifyPropertyChanged
{
    string CodeName { get; }
    int CampaignID { get; }
    int CustomMissionID { get; }
    int CD { get; }
    int Side { get; }
    string Scenario { get; }
    string GUIName { get; }
    string UntranslatedGUIName { get; }
    string IconPath { get; }
    string GUIDescription { get; }
    string FinalMovie { get; }
    bool RequiredAddon { get; }
    bool Enabled { get; }
    bool BuildOffAlly { get; }
    bool PlayerAlwaysOnNormalDifficulty { get; }
    IReadOnlyCollection<string> Tags { get; }
    bool IsCustomMission { get; }
    string PreviewImage { get; }
}
