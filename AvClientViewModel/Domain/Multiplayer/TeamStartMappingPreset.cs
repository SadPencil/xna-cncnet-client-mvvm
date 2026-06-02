using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AvClientViewModel.Domain.Multiplayer
{
    public class TeamStartMappingPreset
    {
        [JsonInclude]
        [JsonPropertyName("n")]
        public string Name { get; set; }

        [JsonInclude]
        [JsonPropertyName("m")]
        public List<TeamStartMapping> TeamStartMappings { get; set; }
    }
}
