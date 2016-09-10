using System.Collections;
using System.Collections.Generic;

using TwitchBot.Enums.Chat;
using TwitchBot.Clients;
using TwitchBot.Models.Bot.Chat;
using TwitchBot.Debugger;
using TwitchBot.Extensions;

namespace TwitchBot.Chat
{
    class Notify
    {
        static TwitchClientOAuth bot;

        static Queue<Message> priv_msg_queue, whisper_queue;

        public static void SetBot(TwitchClientOAuth _bot)
        {
            bot = _bot;
        }

        private static TwitchClientOAuth GetBot()
        {
            return bot;
        }

        public static void SetQueues(Queue<Message> _priv_msg_queue, Queue<Message> _whisper_queue)
        {
            priv_msg_queue = _priv_msg_queue;
            whisper_queue = _whisper_queue;
        }        

        public static void SendMessage(Message message, string response)
        {
            SendResponse(MessageType.Chat, message, response);
        }

        public static void SendWhisper(Message message, string whisper)
        {
            SendResponse(MessageType.Whisper, message, whisper);
        }

        private static void SendResponse(MessageType message_type, Message message, string response)
        {
            GetBot().SendResponse(message_type, message, response);
        }

        public static void Enqueue(DebugMessageType debug_message_type, DebugMethod method, Message template, string notify_object, string object_name = "", string error = "")
        {
            string body = string.Empty,
                   method_string = DebugBot.GetMethodString(method);

            Queue<Message> queue = priv_msg_queue;

            MessageType _message_type = MessageType.Chat;

            Sender _sender = new Sender
            {
                name = GetBot().display_name,
                user_type = UserType.mod
            };

            switch (debug_message_type)
            {
                case DebugMessageType.SUCCESS:
                    {
                        queue = priv_msg_queue;

                        _message_type = MessageType.Chat;

                        body = template.sender.name + " successfully " + method_string + " the " + notify_object;
                    }
                    break;
                case DebugMessageType.ERROR:
                    {
                        queue = whisper_queue;

                        //change the sender name since it will act as the recipient for the whisper
                        _sender.name = template.sender.name;
                        _message_type = MessageType.Whisper;

                        body = "Failed to " + method_string + " the " + notify_object;
                    }
                    break;
                default:
                    break;
            }

            if (!body.CheckString())
            {
                return;
            }

            if (object_name.CheckString())
            {
                body += ", \"" + object_name +"\"";
            }

            if (error.CheckString())
            {
                body += ": " + error;
            }            

            string _key = _message_type == MessageType.Chat ? "PRIVMSG" : "WHISPER";

            Message message = new Message
            {
                key = _key,
                room = template.room,
                message_type = _message_type,
                sender = _sender,
                command = default(Command)
            };

            queue.Enqueue(message);
        }
    }
}
