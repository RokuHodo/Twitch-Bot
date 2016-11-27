using Newtonsoft.Json;

using TwitchBot.Enums.Chat;

namespace TwitchBot.Models.Bot.Spam
{
    class Links
    {
        [JsonProperty("enabled")]
        public bool enabled { get; set; }

        [JsonProperty("permission")]
        public UserType permission { get; set; }
    }
}
