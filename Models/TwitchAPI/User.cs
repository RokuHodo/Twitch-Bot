using System;

using Newtonsoft.Json;

namespace TwitchBot.Models.TwitchAPI
{
    class User
    {
        [JsonProperty("type")]
        public string type { get; set; }

        [JsonProperty("name")]
        public string name { get; set; }

        [JsonProperty("created_at")]
        public DateTime created_at { get; set; }

        [JsonProperty("updated_at")]
        public DateTime updated_at { get; set; }

        [JsonProperty("_links")]
        public Links _links { get; set; }

        [JsonProperty("logo")]
        public string logo { get; set; }

        [JsonProperty("_id")]
        public long _id { get; set; }

        [JsonProperty("display_name")]
        public string display_name { get; set; }

        [JsonProperty("email")]
        public string email { get; set; }

        [JsonProperty("partnered")]
        public bool partnered { get; set; }

        [JsonProperty("bio")]
        public string bio { get; set; }

        [JsonProperty("notifications")]
        public Notifications notifications { get; set; }
    }
}
