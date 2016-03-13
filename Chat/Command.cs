using TwitchChatBot.Enums;

namespace TwitchChatBot.Chat
{
    class Command
    {
        public UserType permission;

        public string command, response;

        public bool permanent;

        public Command(UserType permission, string command, string response, bool permanent)
        {
            this.command = command;
            this.response = response;
            this.permanent = permanent;
            this.permission = permission;                   
        }
    }
}
