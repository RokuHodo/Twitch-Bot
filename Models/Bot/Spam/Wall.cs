using Newtonsoft.Json;

using TwitchChatBot.Enums.Chat;

namespace TwitchChatBot.Models.Bot
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
