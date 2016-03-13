using System;

using TwitchChatBot.Enums;
using TwitchChatBot.Extensions;

namespace TwitchChatBot.Chat
{
    class Sender
    {
        public string name;

        public UserType user_type;

        public Sender(string irc_message)
        {
            name = GetSender(irc_message);

            user_type = GetUserType(irc_message);
        }

        /// <summary>
        /// Gets the <see cref="UserType"/> of the user who sent the Twitch message.
        /// </summary>
        /// <param name="irc_message">The message sent from the IRC.</param>
        /// <returns></returns>
        private UserType GetUserType(string irc_message)
        {
            string type, 
                   user_type_tag = ";user-type";

            int parse_start = irc_message.IndexOf(user_type_tag);

            type = irc_message.TextBetween(';', ' ', parse_start, user_type_tag.Length);

            //if the user-type is empty, return a custom user-type to be used with the UserType enum
            if (!type.CheckString())
            {
                type = "viewer";
            }

            try
            {
                return (UserType)Enum.Parse(typeof(UserType), type);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return UserType.viewer;
            }
        }

        /// <summary>
        /// Gets the name of the user who sent the Twitch message.
        /// </summary>
        /// <param name="irc_message">The message sent from the IRC.</param>
        /// <returns></returns>
        private string GetSender(string irc_message)
        {
            string display_name_tag = ";display-name";

            int parse_start = irc_message.IndexOf(display_name_tag);

            return irc_message.TextBetween(';', ';', parse_start, display_name_tag.Length);
        }
    }
}
