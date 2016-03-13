using RestSharp.Deserializers;

namespace TwitchChatBot.TwitchModels
{
    class Channel
    {
        [DeserializeAs(Name = "mature")]
        public bool mature { get; set; }
        [DeserializeAs(Name = "status")]
        public string status { get; set; }
        [DeserializeAs(Name = "broadcaster_language")]
        public string broadcaster_language { get; set; }
        [DeserializeAs(Name = "display_name")]
        public string display_name { get; set; }
        [DeserializeAs(Name = "game")]
        public string game { get; set; }
        [DeserializeAs(Name = "delay")]
        public long delay { get; set; }
        [DeserializeAs(Name = "language")]
        public string language { get; set; }
        [DeserializeAs(Name = "_id")]
        public long _id { get; set; }
        [DeserializeAs(Name = "name")]
        public string name { get; set; }
        [DeserializeAs(Name = "created_at")]
        public string created_at { get; set; }
        [DeserializeAs(Name = "updated_at")]
        public string updated_at { get; set; }
        [DeserializeAs(Name = "logo")]
        public string logo { get; set; }
        [DeserializeAs(Name = "banner")]
        public string banner { get; set; }
        [DeserializeAs(Name = "video_banner")]
        public string video_banner { get; set; }
        [DeserializeAs(Name = "background")]
        public string background { get; set; }
        [DeserializeAs(Name = "profile_banner")]
        public string profile_banner { get; set; }
        [DeserializeAs(Name = "profile_banner_background_color")]
        public string profile_banner_background_color { get; set; }
        [DeserializeAs(Name = "partner")]
        public bool partner { get; set; }
        [DeserializeAs(Name = "url")]
        public string url { get; set; }
        [DeserializeAs(Name = "views")]
        public int views { get; set; }
        [DeserializeAs(Name = "followers")]
        public int followers { get; set; }
    }
}
