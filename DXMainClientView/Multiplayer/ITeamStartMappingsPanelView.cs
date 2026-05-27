#nullable enable

using System.Collections.Generic;

namespace DXMainClientView;

public interface ITeamStartMappingsPanelView
{
    void Show();
    void Hide();
    void SetMappingCount(int mappingCount);
    void SetMappingLabel(int mappingIndex, string labelText);
    void SetMappingOptions(int mappingIndex, IEnumerable<string> options);
    void SetSelectedMapping(int mappingIndex, string selectedOption);
    void SetMappingEnabled(int mappingIndex, bool enabled);
    void ClearMappings();
}
