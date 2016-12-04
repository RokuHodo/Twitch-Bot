using System.Collections.Generic;

using TwitchBot.Clients;
using TwitchBot.Debugger;
using TwitchBot.Enums.Chat;
using TwitchBot.Enums.Debugger;
using TwitchBot.Messages;
using TwitchBot.Models.Bot.Chat;

using TwitchBot.Extensions;

namespace TwitchBot.Chat
{
    class TwitchNotify
    {
        static TwitchClientOAuth bot;

        static Queue<TwitchMessage> priv_msg_queue, whisper_queue;

        #region Initialization

        /// <summary>
        /// Set's the bot that sends notifies to Twitch.
        /// </summary>
        public static void SetBot(TwitchClientOAuth _bot)
        {
            bot = _bot;
        }

        /// <summary>
        /// Gets the bot that sends the notifies to Twitch.
        /// </summary>
        private static TwitchClientOAuth GetBot()
        {
            return bot;
        }

        /// <summary>
        /// Set the queues to properly send notififes to twitch.
        /// </summary>
        public static void SetQueues(ref Queue<TwitchMessage> _priv_msg_queue, ref Queue<TwitchMessage> _whisper_queue)
        {
            priv_msg_queue = _priv_msg_queue;
            whisper_queue = _whisper_queue;
        }

        #endregion

        #region Sending messages to twitch

        /// <summary>
        /// Wrapper to send a message to twitch chat.
        /// </summary>
        public static void SendMessage(TwitchMessage message, string response)
        {
            SendResponse(MessageType.Chat, message, response);
        }

        /// <summary>
        /// Wrapper to send a whisper to a user.
        /// </summary>
        public static void SendWhisper(TwitchMessage message, string whisper)
        {
            SendResponse(MessageType.Whisper, message, whisper);
        }

        /// <summary>
        /// Sends either a chat message or a whisper to Twitch.
        /// </summary>
        private static void SendResponse(MessageType message_type, TwitchMessage message, string response)
        {
            GetBot().SendResponse(message_type, message, response);
        }

        #endregion

        #region Sending notifies to twitch

        /// <summary>
        /// Sends a success notification to Twitch chat.
        /// </summary>
        public static void Success(DebugMethod method, TwitchMessage message, string obj, string name = "")
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

        /// <summary>
        /// Sends an error whisper to a user.
        /// </summary>
        public static void Error(DebugMethod method, TwitchMessage message, string obj, string name = "", string error = "")
        {
            string body = string.Empty;

            Sender sender = new Sender
            {
                name = message.sender.name,
            };

            body = "Failed to " + method.ToString().ToLower() + " the " + obj;

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

        /// <summary>
        /// Enqueues a message to be sent as a chat message or a whisper.
        /// </summary>
        private static void Enqueue(Queue<TwitchMessage> queue, string room, string body, MessageType message_type, Sender sender)
        {
            if (!body.CheckString())
            {
                return;
            }

            if (sender == default(Sender))
            {
                return;
            }

            TwitchMessage notify = new TwitchMessage
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
