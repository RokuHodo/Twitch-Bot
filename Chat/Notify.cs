using System;

using TwitchChatBot.Enums;
using TwitchChatBot.Enums.Debugger;
using TwitchChatBot.Clients;
using TwitchChatBot.Debugger;

namespace TwitchChatBot.Chat
{
    static class Notify
    {
        /// <summary>
        /// Sends a chat message on a successful operation of adding/editing/removing a command,variable, or quote.
        /// </summary>
        /// <param name="bot">Contains the methods to send the chat message or whisper.</param>
        /// <param name="operation">The operation what was being performed.</param>
        /// <param name="message">Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="value">String to print in addition to the success message.</param>
        public static void Success(TwitchBot bot, DebugMethod operation, Message message, string value)
        {
            string notify = value + " {0} by " + message.sender.name;

            switch (operation)
            {
                case DebugMethod.Add:
                    notify = string.Format(notify, "added");
                    break;
                case DebugMethod.Edit:
                    notify = string.Format(notify, "edited");
                    break;
                case DebugMethod.Remove:
                    notify = string.Format(notify, "removed");
                    break;
                default:
                    notify = "";
                    break;
            }

            bot.SendResponse(MessageType.Chat, message, notify);
        }

        /// <summary>
        /// Sends a whisper to the user who failed adding/editing/removing a command, variable, or quote.
        /// </summary>
        /// <param name="bot">Contains the methods to send the chat message or whisper.</param>
        /// <param name="operation">The operation what was being performed.</param>
        /// <param name="error">The error that occured while trying to perform the operation.</param>
        /// <param name="message">Contains the message sender to send the whisper to.</param>
        /// <param name="value">String to print in addition to the failed message.</param>
        public static void Failed(TwitchBot bot, DebugMethod operation, DebugError error, Message message, string value)
        {
            string notify = "Failed to {0} " + value + ": " + new DebugErrorResponse().GetError(error);

            switch (operation)
            {
                case DebugMethod.Add:
                    notify = string.Format(notify, "add");
                    break;
                case DebugMethod.Edit:
                    notify = string.Format(notify, "edit");
                    break;
                case DebugMethod.Remove:
                    notify = string.Format(notify, "remove");
                    break;
                default:
                    notify = "";
                    break;
            }

            bot.SendResponse(MessageType.Whisper, message, notify);
        }        
    }
}
