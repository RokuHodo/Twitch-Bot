using Newtonsoft.Json;

using TwitchBot.Enums.Chat;

namespace TwitchBot.Models.Bot.Spam
{
    class Wall
    {
        [JsonProperty("enabled")]
        public bool enabled { get; set; }

        [JsonProperty("length")]
        public int length { get; set; }

        [JsonProperty("permission")]
        public UserType permission { get; set; }
    }
}
