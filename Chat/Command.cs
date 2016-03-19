using TwitchChatBot.Enums;

namespace TwitchChatBot.Chat
{
    class Command
    {
        public UserType permission;
        public CommandType type;

        public string key, response;

        public bool permanent;

        public Command(UserType permission, string key, string response, bool permanent, CommandType type)
        {
            this.key = key;
            this.response = response;
            this.permanent = permanent;
            this.permission = permission;                   
            this.type = type;
        }
    }
}
