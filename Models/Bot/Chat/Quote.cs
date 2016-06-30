using System;

using Newtonsoft.Json;

namespace TwitchChatBot.Models.Bot
{
    [JsonObject("Quote")]
    class Quote
    {
        [JsonProperty("quote")]
        public string quote { get; set; }

        [JsonProperty("quotee")]
        public string quotee { get; set; }

        [JsonProperty("date")]
        public DateTime date { get; set; }
    }
}
