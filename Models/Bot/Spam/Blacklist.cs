using System.Collections.Generic;

using Newtonsoft.Json;

using TwitchChatBot.Enums.Chat;

namespace TwitchChatBot.Models.Bot
{
    class Blacklist
    {
        [JsonProperty("enabled")]
        public bool enabled { get; set; }

        [JsonProperty("permission")]
        public UserType permission { get; set; }
    }
}
