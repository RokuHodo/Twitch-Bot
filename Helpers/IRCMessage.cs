using System;
using System.Collections.Generic;

using TwitchBot.Debugger;
using TwitchBot.Enums.Extensions;
using TwitchBot.Extensions;

namespace TwitchBot.Helpers
{
    class IRCMessage
    {
        public Dictionary<string, string> tags { get; set; }

        public string prefix { get; set; }

        public string command { get; set; }

        public string parameters { get; set; }

        public string[] middle;
        public string[] trailing;

        public IRCMessage(string irc_message)
        {
            string irc_message_no_tags = string.Empty;

            tags = GetTags(irc_message, out irc_message_no_tags);
            prefix = GetPrefix(irc_message_no_tags);
            command = GetCommand(irc_message_no_tags);
            parameters = GetParameters(command, irc_message, out middle, out trailing);
        }

        #region Parser functions

        /// <summary>
        /// Searches the tags attached to the message and extracts any that exist and assigns them to the <see cref="Dictionary{TKey, TValue}"/>
        /// </summary>
        /// <param name="possible_tags">A <see cref="Dictionary{TKey, TValue}"/> that contains all of the possoble tags that can be sent with the message.</param>
        /// <param name="key">The <see cref="string"/> that determines what type of twitch message was sent.</param>
        /// <param name="prefix">The part of the message that contains the sender name and the channel/room the message was sent in.</param>
        /// <param name="irc_message">The <see cref="string"/> send from the IRC to be parsed for the tags.</param>
        /// <returns></returns>
        private Dictionary<string, string> GetTags(string irc_message, out string irc_message_no_tags)
        {
            Dictionary<string, string> tags = new Dictionary<string, string>();

            //irc message doesn't contain any tags
            if (!ContainsTags(irc_message))
            {
                irc_message_no_tags = irc_message;

                return tags;
            }

            string tags_extracted = irc_message.TextBetween('@', ' ');

            string[] tags_extracted_array = tags_extracted.StringToArray<string>(';'),
                     tags_array_temp;

            foreach (string tag in tags_extracted_array)
            {
                tags_array_temp = tag.StringToArray<string>('=');

                try
                {
                    tags[tags_array_temp[0]] = tags_array_temp[1];
                }
                catch (Exception exception)
                {
                    DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.GET, "tag", DebugError.NORMAL_EXCEPTION);
                    DebugBot.PrintLine(nameof(exception), exception.Message);
                }
            }

            irc_message_no_tags = irc_message.TextAfter(" ");

            return tags;
        }

        /// <summary>
        /// Gets the prefix before the body of the message that coontains the channel/room the messge was sent in and the name of the sender.
        /// </summary>
        /// <param name="irc_message">The <see cref="string"/> send from the IRC to be parsed into a <see cref="TwitchMessage"/>.</param>
        /// <param name="key_temp">The temporary key to use as a starting point to search for the prefix.</param>
        /// <returns></returns>
        public string GetPrefix(string irc_message)
        {
            return irc_message.TextBefore(" ");
        }

        /// <summary>
        /// Gets the type of message sent through Twitch, i.e. PRIVMSG, WHISEPER, USERNOTICE, etc.
        /// </summary>
        /// <param name="prefix">The prefix before the message sent through Twitch.</param>
        /// <returns></returns>
        private string GetCommand(string irc_message)
        {
            return irc_message.TextBetween(' ', ' ');
        }

        private string GetParameters(string irc_command, string irc_message, out string[] middle, out string[] trailing)
        {
            string parameters = irc_message.TextAfter(irc_command).RemoveWhiteSpace(WhiteSpace.Left);

            //check to see if there is trailing
            if (parameters.IndexOf(":") != -1)
            {
                middle = parameters.TextBefore(":").RemoveWhiteSpace(WhiteSpace.Both).StringToArray<string>(' ');
                trailing = parameters.TextAfter(":").RemoveWhiteSpace(WhiteSpace.Both).StringToArray<string>(' ');
            }
            else
            {
                middle = parameters.StringToArray<string>(' ');
                trailing = new string[0];
            }

            return parameters;
        }

        #endregion

        #region Boolean logic

        private bool ContainsTags(string irc_message)
        {
            return irc_message.StartsWith("@");
        }

        #endregion
    }
}
