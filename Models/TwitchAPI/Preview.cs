using Newtonsoft.Json;

namespace TwitchBot.Models.TwitchAPI
{
    class Preview
    {
        [JsonProperty("small")]
        public string small { get; set; }

        [JsonProperty("medium")]
        public string medium { get; set; }

        [JsonProperty("large")]
        public string large { get; set; }

        [JsonProperty("template")]
        public string template { get; set; }
    }
}
