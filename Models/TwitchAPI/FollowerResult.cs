using System.Collections.Generic;

using Newtonsoft.Json;

namespace TwitchChatBot.Models.TwitchAPI
{
    class FollowerResult
    {
        [JsonProperty("follows")]
        public List<Follower> follows { get; set; }

        [JsonProperty("_total")]
        public int _total { get; set; }

        [JsonProperty("_links")]
        public Links _links { get; set; }

        [JsonProperty("_cursor")]
        public string _cursor { get; set; }
    }
}
