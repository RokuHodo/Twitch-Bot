using System.Collections.Generic;

using TwitchBot.Clients;
using TwitchBot.Debugger;
using TwitchBot.Enums.Chat;
using TwitchBot.Enums.Debugger;
using TwitchBot.Models.Bot.Chat;
using TwitchBot.Parser;

using TwitchBot.Extensions;

namespace TwitchBot.Chat
{
    class TwitchNotify
    {
        static TwitchClientOAuth bot;

        static Queue<MessageTwitch> priv_msg_queue, whisper_queue;

        #region Initialization

        public static void SetBot(TwitchClientOAuth _bot)
        {
            bot = _bot;
        }

        private static TwitchClientOAuth GetBot()
        {
            return bot;
        }

        public static void SetQueues(ref Queue<MessageTwitch> _priv_msg_queue, ref Queue<MessageTwitch> _whisper_queue)
        {
            priv_msg_queue = _priv_msg_queue;
            whisper_queue = _whisper_queue;
        }

        #endregion

        #region Sending messages to twitch

        public static void SendMessage(MessageTwitch message, string response)
        {
            SendResponse(MessageType.Chat, message, response);
        }

        public static void SendWhisper(MessageTwitch message, string whisper)
        {
            SendResponse(MessageType.Whisper, message, whisper);
        }

        private static void SendResponse(MessageType message_type, MessageTwitch message, string response)
        {
            GetBot().SendResponse(message_type, message, response);
        }

        #endregion

        #region Sending notifies to twitch

        public static void Success(DebugMethod method, MessageTwitch message, string obj, string name = "")
        {
            string body = string.Empty;

            Sender sender = new Sender
            {
                name = GetBot().display_name,
                user_type = UserType.mod
            };

            body = message.sender.name + " successfully " + DebugBot.GetSuccessMethodString(method).ToLower() + " the " + obj;

            if (name.CheckString())
            {
                body += ", \"" + name + "\"";
            }

            Enqueue(priv_msg_queue, message.room, body, MessageType.Chat, sender);
        }

        public static void Error(DebugMethod method, MessageTwitch message, string obj, string name = "", string error = "")
        {
            string body = string.Empty;

            Sender sender = new Sender
            {
                name = message.sender.name,
            };

            body = "Failed to " + message.ToString().ToLower() + " the " + obj;

            if (name.CheckString())
            {
                body += ", \"" + name + "\"";
            }

            if (error.CheckString())
            {
                body += ": " + error;
            }

            Enqueue(whisper_queue, message.room, body, MessageType.Whisper, sender);
        }

        private static void Enqueue(Queue<MessageTwitch> queue, string room, string body, MessageType message_type, Sender sender)
        {
            if (!body.CheckString())
            {
                return;
            }

            if (sender == default(Sender))
            {
                return;
            }

            MessageTwitch notify = new MessageTwitch
            {
                room = room,
                body = body,
                message_type = message_type,
                sender = sender,
            };

            queue.Enqueue(notify);
        }

        #endregion
    }
}
