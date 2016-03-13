using System;
using System.Collections.Generic;
using System.Threading;

using TwitchChatBot.Chat;
using TwitchChatBot.Enums;
using TwitchChatBot.Debugger;
using TwitchChatBot.Connection;
using TwitchChatBot.Extensions;

namespace TwitchChatBot.Clients
{
    class TwitchBot
    {
        private double queue_delay = 1;

        private string bot_token,
                       bot_user_name;

        Quotes quotes;

        Variables variables;
        Commands commands;        

        TwitchConnection chat_connection,
                         whisper_connection;

        TwitchUserAuthenticated broadcaster;               

        DateTime last_chat_time = DateTime.Now,
                 last_whisper_time = DateTime.Now;

        Queue<Message> chat_queue = new Queue<Message>(),
                       whisper_queue = new Queue<Message>();

        public TwitchBot(string bot_token, string client_id, TwitchUserAuthenticated broadcaster)
        {
            quotes = new Quotes(broadcaster);

            variables = new Variables();
            commands = new Commands(variables);
            

            bot_user_name = new TwitchUserAuthenticated(client_id, bot_token).GetAuthenticatedUser().name;
            this.bot_token = bot_token;

            this.broadcaster = broadcaster;

            //connect to chat server
            chat_connection = new TwitchConnection(ConnectionType.Chat, bot_user_name, bot_token);

            Thread _ReadChat = new Thread(new ThreadStart(ReadChat));
            _ReadChat.Start();

            Thread.Sleep(350);

            //connect to the whisper sever
            whisper_connection = new TwitchConnection(ConnectionType.Whisper, bot_user_name, bot_token);

            Thread _ReadWhispers = new Thread(new ThreadStart(ReadWhispers));
            _ReadWhispers.Start();            

            Thread.Sleep(350);            

            //so we can speak through the bot
            Thread _BotSpeak = new Thread(new ThreadStart(BotSpeak));
            _BotSpeak.Start();            
        }

        #region Chat and Whisper connection

        /// <summary>
        /// Chet to see if the chat client is connected.
        /// </summary>
        /// <returns></returns>
        public bool ChatConnected()
        {
            return chat_connection.isConnected();
        }

        /// <summary>
        /// Chet to see if the whisper client is connected.
        /// </summary>
        /// <returns></returns>
        public bool WhisperConnected()
        {
            return whisper_connection.isConnected();
        }

        /// <summary>
        /// Connect to the chat server.
        /// </summary>
        public void ConnectChat()
        {
            chat_connection.Connect();
        }

        /// <summary>
        /// Connect to the whisper server.
        /// </summary>
        public void ConnectWhisper()
        {
            whisper_connection.Connect();
        }

        #endregion

        #region Join and Leave a channel

        /// <summary>
        /// Join a channel to moderate.
        /// </summary>
        /// <param name="broadcaster_user_name">Channel to join.</param>
        public void JoinChannel(string broadcaster_user_name)
        {
            Console.WriteLine();
            Debug.Notify("Joining room: " + broadcaster_user_name.ToLower() + Environment.NewLine);

            chat_connection.writer.WriteLine("JOIN #" + broadcaster_user_name.ToLower());
            chat_connection.writer.Flush();
        }

        #endregion

        #region Send Messages or Whispers

        /// <summary>
        /// Sends a message to twitch either as a chat message or whisper message.
        /// </summary>
        /// <param name="message_type">Type of message to send.</param>
        /// <param name="message">Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="response">Response to send.</param>
        public void SendResponse(MessageType message_type, Message message, string response)
        {
            if (message_type == MessageType.Chat)
            {
                SendMessage(message, response);
            }
            else
            {
                SendWhisper(message, response);
            }
        }

        /// <summary>
        /// Send a chat message.
        /// </summary>
        /// <param name="message">Contains the message sroom to send the chat message to.</param>
        /// <param name="response">Response to send.</param>
        private void SendMessage(Message message, string response)
        {
            if (!response.CheckString() || !ChatConnected())
            {
                return;
            }

            if (!message.room.CheckString())
            {
                message.room = broadcaster.name;
            }

            chat_connection.writer.WriteLine(":{0}!{0}@{0}.tmi.twitch.tv PRIVMSG #{1} :{2}", bot_user_name, message.room, response);
            chat_connection.writer.Flush();
        }

        /// <summary>
        /// Send a whisper message.
        /// </summary>
        /// <param name="message">Contains the message sender to send the whisper to.</param>
        /// <param name="response">Response to send.</param>
        private void SendWhisper(Message message, string response)
        {
            if (!response.CheckString() || !WhisperConnected())
            {
                return;
            }

            whisper_connection.writer.WriteLine("PRIVMSG #jtv :/w {0} {1}", message.sender.name, response);
            whisper_connection.writer.Flush();
        }

        #endregion

        #region Process commands

        /// <summary>
        /// Attempts to process a command if there is something to process and if enough time has passed.
        /// </summary>
        /// <param name="bot">Required to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        public void TryProcessCommand(TwitchBot bot)
        {
            //make sure there's something to process
            if (chat_queue.Count > 0 && DateTime.Now - last_chat_time > TimeSpan.FromSeconds(queue_delay))
            {
                ProcessCommand(MessageType.Chat, chat_queue.Dequeue(), bot);

                last_chat_time = DateTime.Now;
            }

            //make sure there's something to process
            if (whisper_queue.Count > 0 && DateTime.Now - last_whisper_time > TimeSpan.FromSeconds(queue_delay))
            {
                ProcessCommand(MessageType.Whisper, whisper_queue.Dequeue(), bot);

                last_whisper_time = DateTime.Now;
            }
        }

        /// <summary>
        /// Process a command in the chat or whisper queue.
        /// </summary>
        /// <param name="message_type">Type of message to send.</param>
        /// <param name="message">Required to send a chat message or whisper by calling <see cref="Notify"/>.Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">Required to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        private void ProcessCommand(MessageType message_type, Message message, TwitchBot bot)
        {           
            UserType permisison = commands.GetPermission(message.command);

            //make sure the user has the correct permission
            //NOTE: a user always has a UserType of "viewer" when sending a whisperm can cause issues if using mod only commands through whispers
            if (message.sender.user_type < permisison)
            {
                SendResponse(MessageType.Whisper, message, $"You need to be a(n) {permisison.ToString()} to use {message.command}");

                return;
            }

            switch (message.command.ToLower())
            {
                case "!addcommand":
                    commands.Add(variables, message, bot);
                    break;
                case "!editcommand":
                    commands.Edit(variables, message, bot);
                    break;
                case "!removecommand":
                    commands.Remove(message, bot);
                    break;
                case "!addvariable":
                    variables.Add(commands, message, bot);
                    break;
                case "!editvariable":
                    variables.Edit(commands, message, bot);
                    break;
                case "!removevariable":
                    variables.Remove(commands, message, bot);
                    break;
                case "!settitle":
                    commands.UpdateStream(StreamSetting.Title, message, broadcaster);
                    break;
                case "!setgame":
                    commands.UpdateStream(StreamSetting.Game, message, broadcaster);
                    break;
                case "!setdelay":
                    commands.UpdateStream(StreamSetting.Delay, message, broadcaster);
                    break;
                case "!uptime":
                    SendResponse(message_type, message, commands.GetUpTime(broadcaster));
                    break;
                case "!howlong":
                    SendResponse(message_type, message, commands.GetHowLong(broadcaster.display_name, message.sender.name));
                    break;
                case "!music":
                    SendResponse(message_type, message, commands.GetCurrentSong());
                    break;
                case "!quote":
                    SendResponse(message_type, message, quotes.GetQuote());
                    break;
                case "!addquote":
                    quotes.Add(commands, message, bot, broadcaster);
                    break;
                default:
                    SendResponse(message_type, message, commands.GetResponse(message.command, variables));
                    break;
            }
        }

        #endregion        

        #region Chat and Whisper threads

        /// <summary>
        /// Monitor messages coming in fromt he chat server
        /// </summary>
        public void ReadChat()
        {
            while (true)
            {
                string irc_message = chat_connection.reader.ReadLine();

                if (!irc_message.CheckString())
                {
                    Console.WriteLine("Null message recieved from the IRC");

                    chat_connection.writer.WriteLine("PONG");
                    chat_connection.writer.Flush();

                    continue;
                }

                //the irc is pinging the bot to see if it's still there, so we need to respond to it
                if (irc_message.StartsWith("PING"))
                {
                    chat_connection.writer.WriteLine("PONG");
                    chat_connection.writer.Flush();

                    Console.WriteLine("\"{0}\" recieved from the chat irc and responded with \"PONG\"", irc_message);

                    continue;
                }

                Message message = new Message(MessageType.Chat, irc_message, commands);

                Console.WriteLine(irc_message);

                //only queue the message if it has a command in it
                if (message.sender != null && message.sender.name.CheckString() && message.command.CheckString())
                {
                    chat_queue.Enqueue(message);
                }
            }
        }

        /// <summary>
        /// Monitor messages coming in fromt he whisper server
        /// </summary>
        public void ReadWhispers()
        {
            while (true)
            {
                string whisper_message = whisper_connection.reader.ReadLine();

                if (!whisper_message.CheckString())
                {
                    Console.WriteLine("Null message recieved from the whisper server");

                    whisper_connection.writer.WriteLine("PONG");
                    whisper_connection.writer.Flush();

                    continue;
                }

                //the irc is pinging the bot to see if it's still there, so we need to respond to it
                if (whisper_message.StartsWith("PING"))
                {
                    whisper_connection.writer.WriteLine("PONG");
                    whisper_connection.writer.Flush();

                    Console.WriteLine("\"{0}\" recieved from the whisper irc and responded with \"PONG\"", whisper_message);

                    continue;
                }

                Console.WriteLine(whisper_message);

                Message message = new Message(MessageType.Whisper, whisper_message, commands);

                //only queue the message if it has a command in it
                if (message.sender != null && message.sender.name.CheckString() && message.command.CheckString())
                {
                    whisper_queue.Enqueue(message);
                }
            }
        }

        /// <summary>
        /// Speak through the bot using the command line
        /// </summary>
        public void BotSpeak()
        {
            while (true)
            {
                string input = Console.ReadLine();

                Message message = new Message(MessageType.Chat, input, commands);

                SendMessage(message, input);
            }
        }

        #endregion
    }
}
