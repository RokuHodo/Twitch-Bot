using System;
using Newtonsoft.Json;

namespace TwitchChatBot.Models.TwitchAPI
{
    class Stream
    {
        [JsonProperty("game")]
        public string game { get; set; }

        [JsonProperty("viewers")]
        public int viewers { get; set; }

        [JsonProperty("average_fps")]
        public double average_fps { get; set; }

        [JsonProperty("delay")]
        public double delay { get; set; }

        [JsonProperty("video_height")]
        public int video_height { get; set; }

        [JsonProperty("is_playlist")]
        public bool is_playlist { get; set; }

        [JsonProperty("created_at")]
        public DateTime created_at { get; set; }

        [JsonProperty("_id")]
        public long _id { get; set; }

        [JsonProperty("channel")]
        public Channel channel { get; set; }
    }
}
