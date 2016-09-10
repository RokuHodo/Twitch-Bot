using Newtonsoft.Json;

using TwitchBot.Enums.Chat;

namespace TwitchBot.Models.Bot.Spam
{
    class ASCII
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
