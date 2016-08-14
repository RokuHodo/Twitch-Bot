using Newtonsoft.Json;

namespace TwitchChatBot.Models.TwitchAPI
{
    class StreamResult
    {
        [JsonProperty("stream")]
        public Stream stream { get; set; }

        [JsonProperty("_links")]
        public Links _links { get; set; }
    }
}
