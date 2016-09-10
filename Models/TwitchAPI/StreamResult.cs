using Newtonsoft.Json;

namespace TwitchBot.Models.TwitchAPI
{
    class StreamResult
    {
        [JsonProperty("stream")]
        public Stream stream { get; set; }

        [JsonProperty("_links")]
        public Links _links { get; set; }
    }
}
