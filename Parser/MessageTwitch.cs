using System;
using System.Collections.Generic;

using TwitchBot.Chat;
using TwitchBot.Clients;
using TwitchBot.Debugger;
using TwitchBot.Enums.Chat;
using TwitchBot.Enums.Debugger;
using TwitchBot.Enums.Extensions;
using TwitchBot.Extensions;
using TwitchBot.Models.Bot.Chat;

namespace TwitchBot.Parser
{
    class MessageTwitch
    {
        public bool is_notification { get; set; }

        public string room { get; set; }
        public string body { get; set; }
        public string command_irc { get; set; }
              
        public MessageType message_type { get; set; }

        public Sender sender { get; set; }
        public Command command { get; set; }

        public Dictionary<string, string> tags { get; set; }
        
        public MessageTwitch()
        {

        }

        public MessageTwitch(MessageIRC message_irc, Commands commands, string broadcaster_name)
        {
            string name;

            UserType user_type;

            room = message_irc.middle[0].TextAfter("#");       
            //room = "sixdegreesofgaming";
            body = GetBody(message_irc.trailing);

            message_type = message_irc.command == "WHISPER" ? MessageType.Whisper : MessageType.Chat;

            name = GetSenderName(message_irc.tags, message_irc.prefix);
            user_type = GetSenderUserType(message_irc.tags, name, broadcaster_name);

            sender = new Sender
            {
                name = name,
                user_type = user_type                
            };

            tags = message_irc.tags;
            command_irc = message_irc.command;

            command = commands.ExtractCommand(body);
        }

        private string GetBody(string[] trailing)
        {
            string body = string.Empty;

            if (!trailing.CheckArray())
            {
                return body;
            }

            foreach (string element in trailing)
            {
                body += " " + element;
            }

            body = body.RemoveWhiteSpace(WhiteSpace.Both);            

            return body;
        }

        private string GetSenderName(Dictionary<string, string> tags, string prefix)
        {
            string name = string.Empty;

            //the user did not set a display name on twitch, pull the name from the irc prefix
            if (!tags.ContainsKey("display-name") || !tags["display-name"].CheckString())
            {
                return prefix.TextBetween(':', '!');
            }

            try
            {
                name = tags.ContainsKey("display-name") ? tags["display-name"] : tags["name"];
            }
            catch(Exception exception)
            {
                DebugBot.Error(DebugMethod.GET, nameof(name), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }            

            return name;
        }

        private UserType GetSenderUserType(Dictionary<string, string> tags, string name, string broadcaster_name)
        {
            UserType user_type = UserType.viewer;

            if(!tags.ContainsKey("user-type") || !tags["user-type"].CheckString())
            {
                return user_type;
            }

            if(name.ToLower() == broadcaster_name.ToLower())
            {
                return UserType.broadcaster;
            }

            try
            {
                Enum.TryParse(tags["user-type"], out user_type);
            }
            catch(Exception exception)
            {
                DebugBot.Error(DebugMethod.GET, nameof(user_type), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }            

            return user_type;
        }

        public MessageTwitch CheckForNotification(MessageTwitch message, TwitchClientOAuth bot)
        {
            if(message.command_irc == "USERNOTICE")
            {
                message.body = "Thank you for the continued support, " + message.tags["display-name"] + "!";
                message.sender.name = bot.display_name;
                message.sender.user_type = UserType.mod;
                message.is_notification = true;
            }
            else if(message.command_irc == "PRIVMSG" && message.sender.name == "twitchnotify" && message.body.IndexOf("just subscribed") != -1)
            {
                string subscriber = message.body.TextBefore(" ");

                message.body = "Thank you for subscribing, " + subscriber + "!";
                message.sender.name = bot.display_name;
                message.sender.user_type = UserType.mod;
                message.is_notification = true;
            }

            return message;
        }
    }
}
