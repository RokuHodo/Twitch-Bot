using System;

using TwitchBot.Chat;
using TwitchBot.Debugger;
using TwitchBot.Messages;

namespace TwitchBot.Messages.Parser
{
    static class MessageParser
    {
        /// <summary>
        /// Parses the message sent from the IRC into a <see cref="TwitchMessage"/>
        /// </summary>
        public static TwitchMessage Parse(Commands commands, string irc_message, string broadcaster_name)
        {
            IRCMessage message_irc = new IRCMessage(irc_message);
            //DebugBot.PrintObject(message_irc);

            TwitchMessage message_twitch;            

            if (message_irc.command == "PRIVMSG" || message_irc.command == "WHISPER" || message_irc.command == "USERNOTICE")
            {
                message_twitch = new TwitchMessage(message_irc, commands, broadcaster_name);

                DebugBot.BlankLine();
                DebugBot.Print("[ " + message_twitch.message_type.ToString() + " ] " + message_twitch.sender.name + " (" + message_twitch.sender.user_type + "): ", ConsoleColor.Magenta);
                DebugBot.PrintLine(message_twitch.body);

                /*
                if(message_twitch.command != default(Command))
                {
                    DebugBot.PrintLine(nameof(message_twitch.command), message_twitch.command.key);
                }
                */
            }
            else
            {
                message_twitch = default(TwitchMessage);
            }

            return message_twitch;
        }        
    }
}
