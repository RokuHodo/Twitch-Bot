using TwitchBot.Enums.Chat;

namespace TwitchBot.Models.Bot.Chat
{
    class Sender
    {
        public string name { get; set; }
        public UserType user_type { get; set; }

        public bool MeetskPermissionRequirement(UserType user_type, UserType requirement)
        {
            return user_type >= requirement;
        }
    }    
}
