
using Newtonsoft.Json;

using TwitchBot.Enums.Chat;


namespace TwitchBot.Models.Bot.Spam
{
    class SpamSettings
    {
        [JsonProperty("enabled")]
        public bool enabled { get; set; }

        [JsonProperty("permission")]
        public UserType permission { get; set; }

        [JsonProperty("timeouts")]
        public int[] timeouts { get; set; }

        [JsonProperty("Caps")]
        public Caps Caps { get; set; }

        [JsonProperty("Links")]
        public Links Links { get; set; }

        [JsonProperty("ASCII")]
        public ASCII ASCII { get; set; }

        [JsonProperty("Wall")]
        public Wall Wall { get; set; }

        [JsonProperty("Blacklist")]
        public Blacklist Blacklist { get; set; }        
    }
}
