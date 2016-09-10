using Newtonsoft.Json;

namespace TwitchBot.Models.TwitchAPI
{
    class Links
    {
        [JsonProperty("self")]
        public string self { get; set; }

        [JsonProperty("next")]
        public string next { get; set; }

        [JsonProperty("channel")]
        public string channel { get; set; }

        [JsonProperty("follows")]
        public string follows { get; set; }

        [JsonProperty("commercial")]
        public string commercial { get; set; }

        [JsonProperty("stream_key")]
        public string stream_key { get; set; }

        [JsonProperty("chat")]
        public string chat { get; set; }

        [JsonProperty("subscriptions")]
        public string subscriptions { get; set; }

        [JsonProperty("editots")]
        public string editots { get; set; }

        [JsonProperty("teams")]
        public string teams { get; set; }

        [JsonProperty("videos")]
        public string videos { get; set; }
    }
}
