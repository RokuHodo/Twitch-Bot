using Newtonsoft.Json;

using TwitchChatBot.Enums.Chat;

namespace TwitchChatBot.Models.Bot
{
    class Caps
    {
        [JsonProperty("enabled")]
        public bool enabled { get; set; }

        [JsonProperty("length")]
        public int length { get; set; }

        [JsonProperty("percent")]
        public int percent { get; set; }

        [JsonProperty("permission")]
        public UserType permission { get; set; }
    }
}
