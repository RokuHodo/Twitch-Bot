using System;
using Newtonsoft.Json;

namespace TwitchChatBot.Models.TwitchAPI
{
    class Follower
    {
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("_links")]
        public Links _links { get; set; }

        [JsonProperty("notifications")]
        public bool notifications { get; set; }

        [JsonProperty("user")]
        public User user { get; set; }
    }
}
