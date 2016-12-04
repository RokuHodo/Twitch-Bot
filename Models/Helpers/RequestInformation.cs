using Newtonsoft.Json;

namespace TwitchBot.Models.Helpers
{
    class RequestInformation
    {
        [JsonProperty("error")]
        public string error { get; set; }

        [JsonProperty("status")]
        public int _status { get; set; }

        [JsonProperty("message")]
        public string message { get; set; }       
    }
}
