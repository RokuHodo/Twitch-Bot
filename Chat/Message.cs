using System;
using System.Collections.Generic;

using TwitchChatBot.Enums;
using TwitchChatBot.Extensions;

namespace TwitchChatBot.Chat
{
    class Message
    {
        string prefix;

        public string body, room, key;
                
        public Sender sender;
        public Command command;

        public Message(MessageType message_type, string irc_message, Commands commands)
        {          
            ParseMessage(message_type, irc_message, commands);
        }

        /// <summary>
        /// Parses a message from the IRC and returns sender, body, room, key, and possible command if the message is a chat message or a whisper.
        /// </summary>
        /// <param name="message_type">Specifies if the chat message should be processed as a whisper or a chat message.</param>
        /// <param name="irc_message">The message sent from the IRC.</param>
        /// <param name="commands">Parses and checks to see if a command was in the IRC message.</param>
        private void ParseMessage(MessageType message_type, string irc_message, Commands commands)
        {
            if (irc_message.Contains("PRIVMSG") || irc_message.Contains("WHISPER"))
            {
                prefix = getPrefix(irc_message);
                key = GetPrefixKey(prefix);

                if (key == "PRIVMSG" || key == "WHISPER")
                {
                    body = GetBody(irc_message, prefix);
                    room = GetRoom(message_type, prefix);
                    command = commands.ExtractCommand(body);

                    sender = new Sender(irc_message);

                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write(Environment.NewLine + $"{sender.name} ({sender.user_type.ToString()}): ");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    Console.Write(body + Environment.NewLine);

                    Console.WriteLine("\tPrefix: \"{0}\"", prefix);
                    Console.WriteLine("\tKey: \"{0}\"", key);

                    Console.WriteLine("\tRoom: \"{0}\"", room);
                    Console.WriteLine("\tCommand: \"{0}\"" + Environment.NewLine, command);                   
                }
            }
        }

        /// <summary>
        /// Gets the prefix before message sent through Twitch
        /// </summary>
        /// <param name="irc_message">The message sent from the IRC.</param>
        /// <returns></returns>
        public string getPrefix(string irc_message)
        {
            int parse_start = irc_message.IndexOf(";user-type");

            return irc_message.TextBetween(':', ':', parse_start);
        }

        /// <summary>
        /// Gets the prefix key within the prefix.
        /// Determines if the IRC message is a chat message, or whisper message.
        /// </summary>
        /// <param name="prefix">The prefix before the message sent through Twitch.</param>
        /// <returns></returns>
        private string GetPrefixKey(string prefix)
        {
            return prefix.TextBetween(' ', ' ');
        }

        /// <summary>
        /// Get the text that the twitch user actually types in chat.
        /// </summary>
        /// <param name="irc_message">The message sent from the IRC.</param>
        /// <param name="prefix">The prefix before the message sent through Twitch.</param>
        /// <returns></returns>
        private string GetBody(string irc_message, string prefix)
        {
            int parse_start = irc_message.IndexOf(prefix) + prefix.Length;

            return irc_message.Substring(parse_start + 1);
        }

        /// <summary>
        /// Get the room that the message was sent from.
        /// </summary>
        /// <param name="message_type">Chat message or a whisper.</param>
        /// <param name="prefix">The prefix before the message sent through Twitch.</param>
        /// <returns></returns>
        private string GetRoom(MessageType message_type, string prefix)
        {
            string room = "";

            if (message_type == MessageType.Chat)
            {
                room = prefix.TextBetween('#', ' ');
            }
            else
            {
                room = prefix.TextBetween(' ', ' '); ;
            }

            return room;
        }               
    }
}
