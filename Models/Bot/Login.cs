using Newtonsoft.Json;

namespace TwitchBot.Models.Bot
{
    class Login
    {
        [JsonProperty("client_id")]
        public string client_id { get; set; }

        [JsonProperty("bot_token")]
        public string bot_token { get; set; }

        [JsonProperty("broadcaster_token")]
        public string broadcaster_token { get; set; }
    }
}
