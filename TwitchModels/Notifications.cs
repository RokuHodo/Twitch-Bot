using RestSharp.Deserializers;

namespace TwitchChatBot.TwitchModels
{
    class Notifications
    {
        [DeserializeAs(Name = "email")]
        public bool email { get; set; }
        [DeserializeAs(Name = "push")]
        public bool push { get; set; }
    }
}
