using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DTAClient.Domain.Multiplayer
{
    public class TeamStartMappingPreset // checked
    {
        [JsonInclude]
        [JsonPropertyName("n")]
        public string Name { get; set; }

        [JsonInclude]
        [JsonPropertyName("m")]
        public List<TeamStartMapping> TeamStartMappings { get; set; }
    }
}
