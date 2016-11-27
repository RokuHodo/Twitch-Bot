using Newtonsoft.Json;

namespace TwitchBot.Models.Bot.Chat
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
