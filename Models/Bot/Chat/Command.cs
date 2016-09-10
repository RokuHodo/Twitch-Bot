using System;

using Newtonsoft.Json;

using TwitchBot.Enums.Chat;

namespace TwitchBot.Models.Bot.Chat
{
    class Command
    {
        [JsonProperty("key")]
        public string key { get; set; }

        [JsonProperty("response")]
        public string response { get; set; }

        [JsonProperty("permanent")]
        public bool permanent { get; set; }

        [JsonProperty("permission")]
        public UserType permission { get; set; }

        [JsonProperty("type")]
        public CommandType type { get; set; }

        [JsonProperty("cooldown")]
        public int cooldown { get; set; }

        [JsonProperty("last_used")]
        public DateTime last_used { get; set; }
    }
}
