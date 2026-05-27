#nullable enable

using System.Collections.Generic;

namespace DXMainClientView;

public interface IPlayerExtraOptionsPanelView
{
    void SetForceRandomSides(bool enabled);
    void SetForceRandomColors(bool enabled);
    void SetForceNoTeams(bool enabled);
    void SetForceRandomStarts(bool enabled);
    void SetUseTeamStartMappings(bool enabled);
    void SetTeamStartMappingsVisible(bool visible);
    void SetTeamMappingsSummary(IEnumerable<string> mappings);
    void SetPanelEnabled(bool enabled);
}
