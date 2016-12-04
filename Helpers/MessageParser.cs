using System;
using System.Collections.Generic;
using System.Linq;

using TwitchBot.Debugger;
using TwitchBot.Chat;
using TwitchBot.Enums.Chat;
using TwitchBot.Extensions;
using TwitchBot.Models.Bot.Chat;

namespace TwitchBot.Helpers
{
    static class MessageParser
    {
        /// <summary>
        /// Parses the <see cref="irc_command"/> into a <see cref="TwitchMessage"/> that can be printed to chat or whispered to a user
        /// </summary>
        /// <param name="commands">Instance of <see cref="Commands"/>, used to extract any command in the message.</param>
        /// <param name="irc_message">The <see cref="string"/> send from the IRC to be parsed into a <see cref="TwitchMessage"/>.</param>
        /// <param name="broadcaster_name">The name of the broadcaster. Ued to assign a special <see cref="MessageType"/> to the streamer.</param>
        /// <returns></returns>
        public static TwitchMessage Parse(Commands commands, string irc_message, string broadcaster_name)
        {
            IRCMessage message_irc = new IRCMessage(irc_message);
            //DebugBot.PrintObject(message_irc);

            TwitchMessage message_twitch;            

            if (message_irc.command == "PRIVMSG" || message_irc.command == "WHISPER" || message_irc.command == "USERNOTICE")
            {
                message_twitch = new TwitchMessage(message_irc, commands, broadcaster_name);

                DebugBot.BlankLine();
                DebugBot.Print(message_twitch.sender.name + " (" + message_twitch.sender.user_type + "): ", ConsoleColor.Cyan);
                DebugBot.PrintLine(message_twitch.body);

                if(message_twitch.command != default(Command))
                {
                    DebugBot.PrintLine(nameof(message_twitch.command), message_twitch.command.key);
                }
            }
            else
            {
                message_twitch = default(TwitchMessage);
            }

            /*
            if (irc_command == "PRIVMSG" || irc_command == "WHISPER")
            {
                //this should be the only situation where "just subscribed" in the message from the notify, still pretty hacky though
                //try and find a better solution
                if (sender.name == "twitchnotify")
                {
                    if (body.Contains("just subscribed"))
                    {
                        string subscriber = body.TextBefore(" ");

                        //change the body of the message to what will be printed in chat by the bot
                        message_twitch.body = "Thank you for subscribing, " + subscriber + "!";

                        DebugBot.PrintLine(subscriber + " just subscribed to " + room + "!", ConsoleColor.Green);
                    }
                }
                else
                {
                    DebugBot.Print(sender.name + "(" + sender.user_type.ToString() + "): ", ConsoleColor.Magenta);
                    DebugBot.PrintLine(body);
                }
            }
            else
            {
                //change the body of the message to what will be printed in chat by the bot
                message_twitch.body = "Thank you for the continued support, " + sender.name + "!";

                DebugBot.PrintLine(sender.name + " just subscribed to " + room + " for " + tags["msg-param-months"] + " months!", ConsoleColor.Green);
            }
            */

            return message_twitch;
        }        
    }
}
