using System;
using System.Text.Json.Serialization;

namespace AvMainClientViewModel.Online
{
    public class RecentPlayer
    {
        [JsonInclude]
        public string PlayerName { get; set; }

        [JsonInclude]
        public string GameName { get; set; }

        [JsonInclude]
        public DateTime GameTime { get; set; }
    }
}
