using System;
using System.Collections.Generic;

using TwitchBot.Debugger;
using TwitchBot.Enums.Debugger;
using TwitchBot.Enums.Extensions;
using TwitchBot.Extensions;

namespace TwitchBot.Messages
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
        /// Searches for tags attached to the irc message and extracts any that exist and extracts them as a dictionary.
        /// </summary>
        private Dictionary<string, string> GetTags(string irc_message, out string irc_message_no_tags)
        {
            Dictionary<string, string> tags = new Dictionary<string, string>();

            //irc message only conmtains tags when it is preceeded with "@"
            if (!irc_message.StartsWith("@"))
            {
                irc_message_no_tags = irc_message;

                return tags;
            }

            //tags exist between "@" an the first space
            string tags_extracted = irc_message.TextBetween('@', ' ');

            //tags are delineated by ";"
            string[] tags_extracted_array = tags_extracted.StringToArray<string>(';'),
                     tags_array_temp;

            foreach (string tag in tags_extracted_array)
            {
                tags_array_temp = tag.StringToArray<string>('=');

                try
                {
                    //there should never be a situation where this fails, but just in case
                    tags[tags_array_temp[0]] = tags_array_temp[1];
                }
                catch (Exception exception)
                {
                    DebugBot.Error(DebugMethod.GET, "tag", DebugError.NORMAL_EXCEPTION);
                    DebugBot.PrintLine(nameof(exception), exception.Message);
                }
            }

            //cut of the tags to make handling the message later easier
            irc_message_no_tags = irc_message.TextAfter(" ");

            return tags;
        }

        /// <summary>
        /// Gets the prefix of the irc message. The irc message passed must have no tags attached.
        /// </summary>
        public string GetPrefix(string irc_message)
        {
            return irc_message.TextBefore(" ");
        }

        /// <summary>
        /// Gets the irc message command. The irc message passed must have no tags attached.
        /// </summary>
        private string GetCommand(string irc_message)
        {
            return irc_message.TextBetween(' ', ' ');
        }

        /// <summary>
        /// Gets the parameters after the irc command and parses for the middle and trialing part of the message. The irc message passed must have no tags attached.
        /// </summary>
        private string GetParameters(string irc_command, string irc_message, out string[] middle, out string[] trailing)
        {
            string parameters = irc_message.TextAfter(irc_command).RemovePadding(Padding.Left);

            //check to see if there is trailing
            if (parameters.IndexOf(":") != -1)
            {
                middle = parameters.TextBefore(":").RemovePadding(Padding.Both).StringToArray<string>(' ');
                trailing = parameters.TextAfter(":").RemovePadding(Padding.Both).StringToArray<string>(' ');
            }
            else
            {
                middle = parameters.StringToArray<string>(' ');
                trailing = new string[0];
            }

            return parameters;
        }

        #endregion
    }
}
