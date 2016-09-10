using System.Collections.Generic;

using TwitchBot.Enums.Chat;

namespace TwitchBot.Models.Bot.Chat
{
    class Message
    {
        public string prefix { get; set; }
        public string key { get; set; }

        public string body { get; set; }
        public string room { get; set; }        

        public MessageType message_type { get; set; }

        public Sender sender { get; set; }
        public Command command { get; set; }

        public Dictionary<string, string> tags { get; set; }
    }
}
