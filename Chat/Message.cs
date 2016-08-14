using System;

using TwitchChatBot.Enums.Chat;
using TwitchChatBot.Extensions;
using TwitchChatBot.Models.Bot;

namespace TwitchChatBot.Chat
{
    class Message
    {
        string prefix;

        public string body, room, key;

        public MessageType message_type;
                
        public Sender sender;
        public Command command;

        public Message()
        {
            prefix = default(string);
            body = default(string);
            room = default(string);
            key = default(string);

            message_type = default(MessageType);

            sender = new Sender();
            command = default(Command);
        }

        public Message(string irc_message, Commands commands, string broadcaster_name)
        {          
            ParseMessage(commands, irc_message, broadcaster_name);
        }

        /// <summary>
        /// Parses a message from the IRC and returns sender, body, room, key, and possible command if the message is a chat message or a whisper.
        /// </summary>
        /// <param name="_message_type">Specifies if the chat message should be processed as a whisper or a chat message.</param>
        /// /// <param name="commands">Parses and checks to see if a command was in the IRC message.</param>
        /// <param name="irc_message">The message sent from the IRC.</param>
        /// <param name="broadcaster_name">Name of the broadcaster. Used to assign s special <see cref="UserType"/> to the streamer.</param>
        private void ParseMessage(Commands commands, string irc_message, string broadcaster_name)
        {
            if (irc_message.Contains("PRIVMSG") || irc_message.Contains("WHISPER"))
            {
                prefix = getPrefix(irc_message);
                key = GetPrefixKey(prefix);

                if (key == "PRIVMSG" || key == "WHISPER")
                {
                    if(key == "PRIVMSG")
                    {
                        message_type = MessageType.Chat;
                    }
                    else
                    {
                        message_type = MessageType.Whisper;
                    }

                    body = GetBody(irc_message, prefix);
                    room = GetRoom(message_type, prefix);
                    command = commands.ExtractCommand(body);

                    sender = new Sender(irc_message, broadcaster_name);

                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write(Environment.NewLine + $"{sender.name} ({sender.user_type.ToString()}): ");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    Console.Write(body + Environment.NewLine);

                    Console.WriteLine("\tPrefix: \"{0}\"", prefix);
                    Console.WriteLine("\tKey: \"{0}\"", key);

                    Console.WriteLine("\tRoom: \"{0}\"", room);

                    if(command != default(Command))
                    {
                        Console.WriteLine("\tCommand: \"{0}\"" + Environment.NewLine, command.key);
                    }                    
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
