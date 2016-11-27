using System;

using TwitchBot.Chat;
using TwitchBot.Debugger;

namespace TwitchBot.Parser
{
    static class MessageParser
    {
        /// <summary>
        /// Parses the <see cref="irc_command"/> into a <see cref="MessageTwitch"/> that can be printed to chat or whispered to a user
        /// </summary>
        /// <param name="commands">Instance of <see cref="Commands"/>, used to extract any command in the message.</param>
        /// <param name="irc_message">The <see cref="string"/> send from the IRC to be parsed into a <see cref="MessageTwitch"/>.</param>
        /// <param name="broadcaster_name">The name of the broadcaster. Ued to assign a special <see cref="MessageType"/> to the streamer.</param>
        /// <returns></returns>
        public static MessageTwitch Parse(Commands commands, string irc_message, string broadcaster_name)
        {
            MessageIRC message_irc = new MessageIRC(irc_message);
            //DebugBot.PrintObject(message_irc);

            MessageTwitch message_twitch;            

            if (message_irc.command == "PRIVMSG" || message_irc.command == "WHISPER" || message_irc.command == "USERNOTICE")
            {
                message_twitch = new MessageTwitch(message_irc, commands, broadcaster_name);

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
                message_twitch = default(MessageTwitch);
            }

            return message_twitch;
        }        
    }
}
