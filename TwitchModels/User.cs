using System;
using RestSharp.Deserializers;

namespace TwitchChatBot.TwitchModels
{
    class User
    {
        [DeserializeAs(Name = "type")]
        public string type { get; set; }
        [DeserializeAs(Name = "name")]
        public string name { get; set; }
        [DeserializeAs(Name = "created_at")]
        public DateTime created_at { get; set; }
        [DeserializeAs(Name = "updated_at")]
        public DateTime updated_at { get; set; }
        [DeserializeAs(Name = "logo")]
        public string logo { get; set; }
        [DeserializeAs(Name = "_id")]
        public long _id { get; set; }
        [DeserializeAs(Name = "display_name")]
        public string display_name { get; set; }
        [DeserializeAs(Name = "email")]
        public string email { get; set; }
        [DeserializeAs(Name = "partnered")]
        public bool partnered { get; set; }
        [DeserializeAs(Name = "bio")]
        public string bio { get; set; }
        [DeserializeAs(Name = "notifications")]
        public Notifications notifications { get; set; }
    }
}
