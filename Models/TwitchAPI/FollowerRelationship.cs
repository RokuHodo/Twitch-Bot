using System;

using Newtonsoft.Json;

using TwitchBot.Helpers;

namespace TwitchBot.Models.TwitchAPI
{
    class FollowerRelationship : RequestInformation
    {
        [JsonProperty("created_at")]
        public DateTime created_at { get; set; }

        [JsonProperty("_links")]
        public Links _links { get; set; }

        [JsonProperty("notifications")]
        public bool notifications { get; set; }

        [JsonProperty("channel")]
        public Channel _cursor { get; set; }
    }
}
