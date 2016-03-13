using RestSharp.Deserializers;

namespace TwitchChatBot.TwitchModels
{
    class StreamResult
    {
        [DeserializeAs(Name = "stream")]
        public Stream stream { get; set; }
    }
}
