using TwitchChatBot.Enums.Chat;
using TwitchChatBot.Enums.Debug;
using TwitchChatBot.Extensions;
using TwitchChatBot.Clients;
using TwitchChatBot.Debugger;

namespace TwitchChatBot.Chat
{
    static class Notify
    {
        private static TwitchClientOAuth bot;

        public static void SetBot(TwitchClientOAuth _bot)
        {
            bot = _bot;
        }

        private static TwitchClientOAuth GetBot()
        {
            return bot;
        }

        public static void SendMessage(Message message, string response)
        {
            SendResponse(MessageType.Chat, message, response);
        }

        /// <summary>
        /// Sends a chat message on a successful operation of adding/editing/removing a command,variable, or quote.
        /// </summary>
        /// <param name="operation">The operation what was being performed.</param>
        /// <param name="debug_object">The object that was successfully processed</param>
        /// <param name="message">Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="success_message">String to print in addition to the success message.</param>
        public static void Success(DebugMethod operation, DebugObject debug_object, string success_message, Message message)
        {
            string notify = message.sender.name;

            switch (operation)
            {               
                case DebugMethod.Remove:
                case DebugMethod.Update:
                case DebugMethod.Retrieve:
                    notify += " " + operation.ToString().Replace("_", " ").ToLower() + "d the " + debug_object.ToString().Replace("_", " ").ToLower();
                    break;
                case DebugMethod.Add:
                case DebugMethod.Edit:
                    notify += " " + operation.ToString().Replace("_", " ").ToLower() + "ed the " + debug_object.ToString().Replace("_", " ").ToLower();
                    break;
                default:
                    notify = "";
                    break;
            }

            if(notify.CheckString() && success_message.CheckString())
            {
                notify += ": " + success_message;
            }

            SendResponse(MessageType.Chat, message, notify);
        }

        public static void SendWhisper(Message message, string whisper)
        {
            SendResponse(MessageType.Whisper, message, whisper);
        }

        /// <summary>
        /// Sends a whisper to the user who failed adding/editing/removing a command, variable, or quote.
        /// </summary>
        /// <param name="bot">Contains the methods to send the chat message or whisper.</param>
        /// <param name="operation">The operation what was being performed.</param>
        /// <param name="error">The error that occured while trying to perform the operation.</param>
        /// <param name="message">Contains the message sender to send the whisper to.</param>
        /// <param name="value">String to print in addition to the failed message.</param>
        public static void Error(DebugMethod operation, DebugObject debug_object, string error_message, DebugError error, Message message, int value = 0)
        {
            string notify = "Failed to ";

            switch (operation)
            {
                case DebugMethod.Add:
                case DebugMethod.Edit:
                case DebugMethod.Remove:
                case DebugMethod.Update:
                case DebugMethod.Retrieve:
                    notify += operation.ToString().Replace("_", " ").ToLower() + " the " + debug_object.ToString().Replace("_", " ").ToLower() + " \"" + error_message + "\": " + new ErrorResponse().GetError(error);
                    break;
                default:
                    notify = "";
                    break;
            }

            SendResponse(MessageType.Whisper, message, notify);
        }

        private static void SendResponse(MessageType message_type, Message message, string response)
        {
            GetBot().SendResponse(message_type, message, response);
        }
    }
}
