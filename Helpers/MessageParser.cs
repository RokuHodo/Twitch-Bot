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
        /// Parses the <see cref="irc_command"/> into a <see cref="Message"/> that can be printed to chat or whispered to a user
        /// </summary>
        /// <param name="commands">Instance of <see cref="Commands"/>, used to extract any command in the message.</param>
        /// <param name="irc_message">The <see cref="string"/> send from the IRC to be parsed into a <see cref="Message"/>.</param>
        /// <param name="broadcaster_name">The name of the broadcaster. Ued to assign a special <see cref="MessageType"/> to the streamer.</param>
        /// <returns></returns>
        public static Message Parse(Commands commands, string irc_message, string broadcaster_name)
        {
            string prefix,
                   key,
                   body,
                   room,
                   key_temp;

            MessageType message_type = MessageType.Chat;

            Sender sender;
            Command command;
            Message message;

            Dictionary<string, string> tags = new Dictionary<string, string>();

            string[] keys = new string[] { "PRIVMSG", "WHISPER", "USERNOTICE" };

            if (!irc_message.Contains(keys, out key_temp))
            {
                return default(Message);
            }

            prefix = GetPrefix(irc_message, key_temp);
            key = GetKey(prefix);

            if (!key.CheckString())
            {
                return default(Message);
            }

            tags = GetPossibleTags(key);
            tags = GetTags(tags, prefix, key, irc_message);

            message_type = GetMessageType(key);

            body = GetBody(prefix, key, irc_message);
            room = GetRoom(prefix, key);

            sender = new Sender
            {
                name = GetSenderName(tags, prefix),
                user_type = GetSenderUserType(tags)
            };

            if (sender.name.ToLower() == broadcaster_name.ToLower())
            {
                sender.user_type = UserType.broadcaster;
            }

            command = commands.ExtractCommand(body);            

            message = new Message
            {
                prefix = prefix,
                key = key,
                body = body,
                room = room,
                message_type = message_type,
                sender = sender,
                tags = tags,
                command = command
            };

            if (key == "PRIVMSG" || key == "WHISPER")
            {
                //this should be the only situation where "subscribe" in the message from the notify, still pretty hacky though
                //try and find a better solution
                if (sender.name == "twitchnotify")
                {
                    if (body.Contains("subscribed"))
                    {
                        string subscriber = body.TextBefore(" ");

                        //change the body of the message to what will be printed in chat by the bot
                        message.body = "Thank you for subscribing, " + subscriber + "!";

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
                message.body = "Thank you for the continued support, " + sender.name + "!";

                DebugBot.PrintLine(sender.name + " just subscribed to " + room + " for " + tags["msg-param-months"] + " months!", ConsoleColor.Green);
            }

            DebugBot.PrintLine(nameof(prefix), prefix);
            DebugBot.PrintLine(nameof(key), key);
            DebugBot.PrintLine(nameof(room), room);

            if (command != default(Command))
            {
                DebugBot.PrintLine(nameof(command), command.key);
            }

            return message;
        }

        /// <summary>
        /// Gets the prefix before the body of the message that coontains the channel/room the messge was sent in and the name of the sender.
        /// </summary>
        /// <param name="irc_message">The <see cref="string"/> send from the IRC to be parsed into a <see cref="Message"/>.</param>
        /// <param name="key_temp">The temporary key to use as a starting point to search for the prefix.</param>
        /// <returns></returns>
        public static string GetPrefix(string irc_message, string key_temp)
        {
            int index_start = 0;

            string result = string.Empty;

            switch (key_temp)
            {
                //private messages and whispers
                case "PRIVMSG":
                case "WHISPER":
                    {
                        index_start = irc_message.IndexOf(";user-type");

                        //the only case (as far as I know) where ";user-type" won't be found is when sent by "twitchnotify" for a resub
                        if (index_start == -1)
                        {
                            result = irc_message.TextBetween(':', ':');
                        }
                        else
                        {
                            result = irc_message.TextBetween(':', ':', index_start);
                        }
                    }
                    break;
                //resub notices fromt he user
                case "USERNOTICE":
                    {
                        index_start = irc_message.IndexOf(";user-type");
                        result = irc_message.TextBetween(':', ':', index_start);

                        //there is no second ":" when the user hits resub-without a message
                        if (!result.CheckString())
                        {
                            result = irc_message.TextAfter(":");
                        }
                    }
                    break;
                default:
                    break;
            }

            return result;
        }

        /// <summary>
        /// Gets the type of message sent through Twitch, i.e. PRIVMSG, WHISEPER, USERNOTICE, etc.
        /// </summary>
        /// <param name="prefix">The prefix before the message sent through Twitch.</param>
        /// <returns></returns>
        private static string GetKey(string prefix)
        {
            return prefix.TextBetween(' ', ' ');
        }

        /// <summary>
        /// Gets all of the possible message tags that can be contained within the message based on the key and returns them in a <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="key">The <see cref="string"/> that determines what type of twitch message was sent.</param>
        /// <returns></returns>
        private static Dictionary<string, string> GetPossibleTags(string key)
        {
            Dictionary<string, string> possible_tags = new Dictionary<string, string>();

            possible_tags["badges"] = string.Empty;
            possible_tags["color"] = string.Empty;
            possible_tags["display-name"] = string.Empty;
            possible_tags["emotes"] = string.Empty;
            possible_tags["turbo"] = string.Empty;
            possible_tags["user-id"] = string.Empty;
            possible_tags["user-type"] = string.Empty;

            switch (key)
            {
                case "PRIVMSG":
                    {
                        possible_tags["mod"] = string.Empty;
                        possible_tags["room-id"] = string.Empty;
                        possible_tags["subscriber"] = string.Empty;
                    }
                    break;
                case "WHISPER":
                    {
                        possible_tags["message-id"] = string.Empty;
                        possible_tags["thread-id"] = string.Empty;                        
                    }
                    break;
                case "USERNOTICE":
                    {
                        possible_tags["mod"] = string.Empty;
                        possible_tags["room-id"] = string.Empty;
                        possible_tags["subscriber"] = string.Empty;
                        possible_tags["msg-id"] = string.Empty;
                        possible_tags["msg-param-months"] = string.Empty;
                        possible_tags["system-msg"] = string.Empty;
                        possible_tags["login"] = string.Empty;
                    }
                    break;
                default:
                    break;
            }

            return possible_tags;
        }

        /// <summary>
        /// Searches the tags attached to the message and extracts any that exist and assigns them to the <see cref="Dictionary{TKey, TValue}"/>
        /// </summary>
        /// <param name="possible_tags">A <see cref="Dictionary{TKey, TValue}"/> that contains all of the possoble tags that can be sent with the message.</param>
        /// <param name="key">The <see cref="string"/> that determines what type of twitch message was sent.</param>
        /// <param name="prefix">The part of the message that contains the sender name and the channel/room the message was sent in.</param>
        /// <param name="irc_message">The <see cref="string"/> send from the IRC to be parsed for the tags.</param>
        /// <returns></returns>
        private static Dictionary<string, string> GetTags(Dictionary<string, string> possible_tags, string prefix, string key, string irc_message)
        {
            int index;

            char start,
                 end;

            string _tag,
                   tag_string = irc_message.TextBefore(prefix);

            string[] tags_array = possible_tags.Keys.ToArray();

            foreach (string tag in tags_array)
            {
                start = tag == "badges" ? '@' : ';';
                end = tag == "user-type" ? ' ' : ';';                

                _tag = start + tag;

                try
                {
                    index = tag_string.IndexOf(_tag);

                    if(index == -1)
                    {
                        continue;
                    }

                    possible_tags[tag] = irc_message.TextBetween(start, end, index, _tag.Length);
                }
                catch(Exception exception)
                {
                    DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.GET, nameof(tag), DebugError.NORMAL_EXCEPTION);
                    DebugBot.PrintLine(nameof(exception), exception.Message);
                }             
            }

            return possible_tags;
        }

        /// <summary>
        /// Gets whether the irc_message was sent through chat or through a whisper.
        /// </summary>
        /// <param name="key">The <see cref="string"/> that determines what type of twitch message was sent.</param>
        /// <returns></returns>
        private static MessageType GetMessageType(string key)
        {
            MessageType type = MessageType.Chat;

            switch (key)
            {
                //private messages and whispers
                case "PRIVMSG":
                case "USERNOTICE":
                    {
                        type = MessageType.Chat;
                    }
                    break;
                case "WHISPER":
                    {
                        type = MessageType.Whisper;
                    }
                    break;
                default:
                    break;
            }

            return type;
        }

        /// <summary>
        /// Gets the text that was sent by the user or by twitch. 
        /// </summary>
        /// <param name="irc_message">The <see cref="string"/> send from the IRC to be parsed for the tags.</param>        
        /// <param name="prefix">The prefix before the message sent through Twitch.</param>
        /// <param name="key">The <see cref="string"/> that determines what type of twitch message was sent.</param>
        /// <returns></returns>
        private static string GetBody(string prefix, string key, string irc_message)
        {
            string result = string.Empty;

            switch (key)
            {
                case "PRIVMSG":
                case "WHISPER":
                case "USERNOTICE":
                    {
                        //even if the resub notice doesn't contain a second ":", it will return an empty string, which is technically the actual "message" because they're isn't one
                        result = irc_message.TextAfter(prefix + ":");
                    }
                    break;
                default:
                    break;
            }

            return result;
        }

        /// <summary>
        /// Get the channel/room that the message was sent in/from.
        /// </summary>        
        /// <param name="prefix">The prefix before the message sent through Twitch.</param>
        /// <param name="key">The <see cref="string"/> that determines what type of twitch message was sent.</param>
        /// <returns></returns>
        private static string GetRoom(string prefix, string key)
        {
            string room = "";

            switch (key)
            {
                case "PRIVMSG":
                    {
                        room = prefix.TextBetween('#', ' ');
                    }
                    break;
                case "USERNOTICE":
                    {
                        room = prefix.TextBetween('#', ' ');

                        if (!room.CheckString())
                        {
                            room = prefix.TextAfter("#");
                        }
                    }
                    break;
                case "WHISPER":
                    {
                        room = prefix.TextBetween(' ', ' ');
                    }
                    break;
                default:
                    break;
            }

            return room;
        }

        /// <summary>
        /// Gets the name of the person/thing that sent the message.
        /// </summary>
        /// <param name="tags">A <see cref="Dictionary{TKey, TValue}"/> contianing all of the extracted tags sent with the message.</param>
        /// <param name="prefix">The prefix before the message sent through Twitch.</param>
        /// <returns></returns>
        private static string GetSenderName(Dictionary<string, string> tags, string prefix)
        {
            string name = string.Empty;

            if (!tags.ContainsKey("display-name") || !tags["display-name"].CheckString())
            {
                name = prefix.TextBetween('!', '@');

                return name;
            }

            try
            {
                name = tags["display-name"];
            }
            catch (Exception exception)
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.GET, nameof(name), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }

            return name;
        }

        /// <summary>
        /// Gets the <see cref="UserType"/> of the user who sent the message.
        /// </summary>
        /// /// <param name="tags">A <see cref="Dictionary{TKey, TValue}"/> contianing all of the extracted tags sent with the message.</param>
        /// <returns></returns>
        private static UserType GetSenderUserType(Dictionary<string, string> tags)
        {
            UserType user_type = UserType.viewer;

            //either there was no user-type or there was not a tag to parse
            if(!tags.ContainsKey("user-type") || !tags["user-type"].CheckString())
            {
                return user_type;
            }            

            try
            {
                user_type = (UserType)Enum.Parse(typeof(UserType), tags["user-type"]);
            }
            catch (Exception exception)
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.GET, nameof(user_type), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }

            return user_type;
        }        
    }
}
