using System;
using RestSharp.Deserializers;

namespace TwitchChatBot.TwitchModels
{
    class Stream
    {
        [DeserializeAs(Name = "game")]
        public string game { get; set; }
        [DeserializeAs(Name = "viewers")]
        public int viewers { get; set; }
        [DeserializeAs(Name = "average_fps")]
        public double average_fps { get; set; }
        [DeserializeAs(Name = "delay")]
        public double delay { get; set; }
        [DeserializeAs(Name = "video_height")]
        public int video_height { get; set; }
        [DeserializeAs(Name = "is_playlist")]
        public bool is_playlist { get; set; }
        [DeserializeAs(Name = "created_at")]
        public DateTime created_at { get; set; }
        [DeserializeAs(Name = "_id")]
        public long _id { get; set; }

        [DeserializeAs(Name = "channel")]
        public Channel channel { get; set; }
    }
}
