using System;
using System.Collections.Generic;
using System.Threading;

using TwitchChatBot.Chat;
using TwitchChatBot.Enums.Chat;
using TwitchChatBot.Debugger;
using TwitchChatBot.Extensions;

namespace TwitchChatBot.Clients
{
    class Bot
    {
        private double queue_delay = 0.5; //could use the minumum 0.3 value but use a slightly higher one just to be safe 

        private DateTime last_command_time;

        Quotes quotes;
        Variables variables;
        Commands commands;
        SpamFilter spam_filter;

        Queue<Message> message_queue;             

        TwitchClientOAuth bot,
                          broadcaster;

        public Bot(string bot_token, string client_id, TwitchClientOAuth _broadcaster)
        {
            last_command_time = DateTime.MinValue;

            quotes = new Quotes();
            variables = new Variables();
            commands = new Commands(variables);
            spam_filter = new SpamFilter();

            message_queue = new Queue<Message>();            

            bot = new TwitchClientOAuth(client_id, bot_token);
            Notify.SetBot(bot);

            broadcaster = _broadcaster;

            Thread.Sleep(100);

            Thread _MonitorConnection_Bot = new Thread(new ThreadStart(MonitorConnection_Bot));
            _MonitorConnection_Bot.Start();

            Thread _MonitorConnection_Broadcaster = new Thread(new ThreadStart(MonitorConnection_Broadcaster));
            _MonitorConnection_Broadcaster.Start();

            Thread.Sleep(100);

            Thread _BotSpeak = new Thread(new ThreadStart(BotSpeak));
            _BotSpeak.Start();            
        }

        #region Join and Leave a channel

        /// <summary>
        /// Join a channel to moderate.
        /// </summary>
        /// <param name="broadcaster_user_name">Channel to join.</param>
        public void JoinChannel(string broadcaster_user_name)
        {
            Console.WriteLine();
            Debug.Notify("Joining room: " + broadcaster_user_name.ToLower() + Environment.NewLine);

            bot.connection.writer.WriteLine("JOIN #" + broadcaster_user_name.ToLower());
            bot.connection.writer.Flush();
        }

        #endregion

        #region Process commands

        /// <summary>
        /// Attempts to process a command if there is something to process and if enough time has passed.
        /// </summary>
        /// <param name="bot">Required to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        public void TryProcessCommand()
        {
            if (DateTime.Now - last_command_time > TimeSpan.FromSeconds(queue_delay) && message_queue.Count > 0)
            {
                Message message = message_queue.Dequeue();

                if(CheckPermission(message.message_type, message))
                {
                    ProcessCommand(MessageType.Chat, message);

                    last_command_time = DateTime.Now;
                }                                                     
            }
        }

        /// <summary>
        /// Checks to make sure a user has the right permission level and is using the command in the right chat room.
        /// </summary>
        /// <param name="message_type">Where the command should be called in.</param>
        /// <param name="message">The message that contains the command information.</param>
        /// <returns></returns>
        private bool CheckPermission(MessageType message_type, Message message)
        {
            UserType permisison = message.command.permission;

            //make sure the user has the correct permission
            //NOTE: a user always has a UserType of "viewer" when sending a whisperm can cause issues if using mod only commands through whispers
            if (message.sender.user_type < permisison)
            {
                bot.SendWhisper(message.sender.name, $"You need to be a(n) {permisison.ToString()} to use {message.command.key}");

                return false;
            }

            if (message_type.ToString() != message.command.type.ToString() && message.command.type != CommandType.Both)
            {
                bot.SendMessage(message.room, $"{message.command.key} can only be used through a {message.command.type.ToString().ToLower()} message");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Process a command in the chat or whisper queue.
        /// </summary>
        /// <param name="message_type">Type of message to send.</param>
        /// <param name="message">Required to send a chat message or whisper by calling <see cref="Notify"/>.Contains the message sender and room to send the chat message or whisper.</param>
        private void ProcessCommand(MessageType message_type, Message message)
        {                     
            commands.ResetLastUsed(message.command);

            switch (message.command.key.ToLower())
            {
                case "!addcommand":
                    commands.Add(variables, message);
                    break;
                case "!editcommand":
                    commands.Edit(variables, message);
                    break;
                case "!removecommand":
                    commands.Remove(variables, message);
                    break;
                case "!addvariable":
                    variables.Add(commands, message);
                    break;
                case "!editvariable":
                    variables.Edit(commands, message);
                    break;
                case "!removevariable":
                    variables.Remove(commands, message);
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
                    bot.SendResponse(message_type, message, commands.GetUpTime(broadcaster));
                    break;
                case "!howlong":
                    bot.SendResponse(message_type, message, commands.GetHowLong(broadcaster.display_name, message.sender.name));
                    break;
                case "!music":
                    bot.SendResponse(message_type, message, commands.GetCurrentSong());
                    break;
                case "!quote":
                    bot.SendResponse(message_type, message, quotes.GetQuote());
                    break;
                case "!addquote":
                    quotes.Add(commands, message, broadcaster);
                    break;
                case "!setfilter":
                    spam_filter.ChangeSetting(message, commands);
                    break;
                default:
                    bot.SendResponse(message_type, message, commands.GetResponse(message.command.key, variables));
                    break;
            }
        }

        #endregion        

        #region Chat and Whisper threads

        /// <summary>
        /// Monitor messages coming in fromt he chat server
        /// </summary>
        public void MonitorConnection_Bot()
        {
            string irc_message = string.Empty;

            while (true)
            {
                irc_message = bot.connection.reader.ReadLine();

                if (!bot.connection.isConnected())
                {
                    Debug.Notify("IRC connection for \"" + bot.name + " is lost. Reconnecting...");

                    bot.connection.Connect();

                    continue;
                }

                if (!irc_message.CheckString())
                {
                    Debug.Notify("Null message recieved from the IRC connection for \"" + bot.name + "\"");

                    bot.connection.writer.WriteLine("PONG");
                    bot.connection.writer.Flush();

                    continue;
                }

                //the irc is pinging the bot to see if it's still there, so we need to respond to it
                if (irc_message.StartsWith("PING"))
                {
                    Debug.Notify("\"" + irc_message + "\" recieved from the \"" + bot.name + "\" IRC connection and responded with \"PONG\"");

                    bot.connection.writer.WriteLine("PONG");
                    bot.connection.writer.Flush();
                }

                Message message = new Message(MessageType.Chat, irc_message, commands, broadcaster.name);                               

                Console.WriteLine(irc_message);

                if (message.sender != null && message.sender.name.CheckString())
                {
                    bool message_pass = spam_filter.MessagePasses(message, bot, broadcaster);

                    if (message.command.key.CheckString())
                    {
                        TimeSpan cooldown = TimeSpan.FromSeconds(message.command.cooldown),
                                 cooldown_passed = DateTime.Now - message.command.last_used;

                        if (cooldown_passed.TotalSeconds < cooldown.TotalSeconds)
                        {
                            TimeSpan cooldown_left = cooldown - cooldown_passed;

                            bot.SendWhisper(message.sender.name, $"{message.command.key} has a {cooldown.TotalSeconds.ToString("0.00")} second cooldown and can be used in {cooldown_left.TotalSeconds.ToString("0.00")} second(s)");
                          
                            continue;
                        }

                        message_queue.Enqueue(message);
                    }                    
                }
            }
        }

        public void MonitorConnection_Broadcaster()
        {
            string irc_message = string.Empty;

            while (true)
            {
                irc_message = broadcaster.connection.reader.ReadLine();

                if (!broadcaster.connection.isConnected())
                {
                    Debug.Notify("IRC connection for \"" + broadcaster.name + " is lost. Reconnecting...");

                    broadcaster.connection.Connect();

                    continue;
                }

                if (!irc_message.CheckString())
                {
                    Debug.Notify("Null message recieved from the IRC connection for \"" + broadcaster.name + "\"");

                    broadcaster.connection.writer.WriteLine("PONG");
                    broadcaster.connection.writer.Flush();

                    continue;
                }

                //the irc is pinging the bot to see if it's still there, so we need to respond to it
                if (irc_message.StartsWith("PING"))
                {
                    Debug.Notify("\"" + irc_message + "\" recieved from the \"" + broadcaster.name + "\" IRC connection and responded with \"PONG\"");

                    broadcaster.connection.writer.WriteLine("PONG");
                    broadcaster.connection.writer.Flush();
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

                Message message = new Message(MessageType.Chat, input, commands, broadcaster.name);

                bot.SendMessage(broadcaster.name, input);
            }
        }

        #endregion
    }
}
