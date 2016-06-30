using Newtonsoft.Json;

namespace TwitchChatBot.Models.Bot
{
    [JsonObject("Variable")]
    class Variable
    {
        [JsonProperty("key")]
        public string key { get; set; }

        [JsonProperty("value")]
        public string value { get; set; }
    }
}
