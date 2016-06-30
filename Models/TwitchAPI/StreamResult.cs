using Newtonsoft.Json;

namespace TwitchChatBot.Models.TwitchAPI
{
    class StreamResult
    {
        [JsonProperty("stream")]
        public Stream stream { get; set; }
    }
}
