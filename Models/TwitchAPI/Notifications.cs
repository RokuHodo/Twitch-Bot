using Newtonsoft.Json;

namespace TwitchBot.Models.TwitchAPI
{
    class Notifications
    {
        [JsonProperty("email")]
        public bool email { get; set; }

        [JsonProperty("push")]
        public bool push { get; set; }
    }
}
