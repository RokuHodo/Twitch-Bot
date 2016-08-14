using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using System.Diagnostics;

using TwitchChatBot.Chat;
using TwitchChatBot.Debugger;
using TwitchChatBot.Enums.Chat;
using TwitchChatBot.Extensions;
using TwitchChatBot.Models.Bot;
using TwitchChatBot.Models.TwitchAPI;

namespace TwitchChatBot.Clients
{
    class Bot
    {
        //PRIVATE
        readonly double WHISPER_DELAY = 400,            //using 300 is the thresh hold where whispers could potentially be droped, use a higher value for safety
                        PRIVATE_MSG_DELAY = 500,        //using 300 is the absolute fasted that messages could be sent without getting globalled, use 500 in case the broadcatser want to talk as well
                        FOLLOWER_ALERT_DELAY = 10000;   //check once every 10 seconds in case overhead delays the notification 

        DateTime last_private_msg_sent,
                 last_follower_check;

        Quotes quotes;
        Variables variables;
        Commands commands;
        SpamFilter spam_filter;

        Queue<Message> whisper_queue,
                       private_msg_queue;               
                
        List<Follower> followers_at_launch;

        List<string> followers_added,
                     followers_at_launch_string;

        //PUBLIC
        public TwitchClientOAuth bot,
                                 broadcaster;

        public Bot(string bot_token, string client_id, TwitchClientOAuth _broadcaster)
        {
            last_private_msg_sent = DateTime.Now;
            last_follower_check = DateTime.Now;

            quotes = new Quotes();
            variables = new Variables();
            commands = new Commands(variables);
            spam_filter = new SpamFilter();

            whisper_queue = new Queue<Message>();
            private_msg_queue = new Queue<Message>();

            followers_at_launch_string = new List<string>();
            followers_added = new List<string>();

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

            //get the list of all users following the broadcaster
            followers_at_launch = broadcaster.GetFollowers_All(broadcaster.name).ToList();

            BotDebug.Header("Followers");
            foreach (Follower follower in followers_at_launch)
            {
                followers_at_launch_string.Add(follower.user.display_name);

                BotDebug.PrintLine("follower", follower.user.display_name);
            }
        }

        #region Join and Leave a channel

        /// <summary>
        /// Join a channel to moderate.
        /// </summary>
        /// <param name="broadcaster_user_name">Channel to join.</param>
        public void JoinChannel(string broadcaster_user_name)
        {
            Console.WriteLine();
            BotDebug.Notify("Joining room: " + broadcaster_user_name.ToLower() + Environment.NewLine);

            bot.connection.writer.WriteLine("JOIN #" + broadcaster_user_name.ToLower());
            bot.connection.writer.Flush();
        }

        #endregion

        #region Process commands

        /// <summary>
        /// Attempts to process a command if there is something to process and if enough time has passed.
        /// </summary>
        public void TrySendingPrivateMessage()
        {
            if(DateTime.Now - last_private_msg_sent < TimeSpan.FromMilliseconds(PRIVATE_MSG_DELAY) || private_msg_queue.Count == 0)
            {
                return;
            }

            Message message = private_msg_queue.Dequeue();

            if(message.command != default(Command))
            {
                if (CheckPermission(message.message_type, message))
                {
                    ProcessCommand(MessageType.Chat, message);                    
                }
            }
            else
            {
                bot.SendResponse(MessageType.Chat, message, message.body);
            }

            last_private_msg_sent = DateTime.Now;
        }

        public void TrySendingWhisper()
        {
            if (DateTime.Now - last_private_msg_sent < TimeSpan.FromMilliseconds(WHISPER_DELAY) || whisper_queue.Count == 0)
            {
                return;
            }

            Message message = whisper_queue.Dequeue();

            if (message.command != default(Command))
            {
                if (CheckPermission(message.message_type, message))
                {
                    ProcessCommand(MessageType.Whisper, message);
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
            //NOTE: a user always has a UserType of "viewer" when sending a whisper can cause issues if using mod only commands through whispers
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
                case "!command":
                    commands.Modify(variables, message);
                    break;
                case "!variable":
                    variables.Modify(commands, message);
                    break;
                case "!quote":
                    quotes.Modify(commands, message, broadcaster, bot);
                    break;
                case "!quotes":
                    bot.SendResponse(message_type, message, quotes.GetTotalQuotes());
                    break;
                case "!blacklist":
                    spam_filter.Modify_BlacklistedWords(commands, message);
                    break;
                case "!settitle":
                    broadcaster.UpdateStream(StreamSetting.Title, commands, message);
                    break;
                case "!setgame":
                    broadcaster.UpdateStream(StreamSetting.Game, commands, message);
                    break;
                case "!setdelay":
                    broadcaster.UpdateStream(StreamSetting.Delay, commands, message);
                    break;
                case "!uptime":
                    bot.SendResponse(message_type, message, broadcaster.GetUpTime());
                    break;
                case "!howlong":
                    bot.SendResponse(message_type, message, commands.GetHowLong(broadcaster.display_name, message.sender.name));
                    break;
                case "!music":
                    bot.SendResponse(message_type, message, commands.GetCurrentSong());
                    break;
                case "!setfilter":
                    spam_filter.ChangeSetting(message, commands);
                    break;
                case "!commands":
                    string commands_ = commands.GetCommands();

                    bot.SendResponse(message_type, message, commands_);
                    break;
                default:
                    bot.SendResponse(message_type, message, commands.GetResponse(message.command.key, variables));
                    break;
            }
        }

        #endregion        

        #region Chat and Whisper threads

        /// <summary>
        /// Monitor messages coming in from the bot connection.
        /// </summary>
        private void MonitorConnection_Bot()
        {
            string irc_message = string.Empty;

            while (true)
            {
                irc_message = bot.connection.reader.ReadLine();

                if (!bot.connection.isConnected())
                {
                    BotDebug.Notify("IRC connection for \"" + bot.name + " is lost. Reconnecting...");

                    bot.connection.Connect();

                    continue;
                }

                if (!irc_message.CheckString())
                {
                    BotDebug.Notify("Null message recieved from the IRC connection for \"" + bot.name + "\"");

                    bot.connection.writer.WriteLine("PONG");
                    bot.connection.writer.Flush();

                    continue;
                }

                //the irc is pinging the bot to see if it's still there, so we need to respond to it
                if (irc_message.StartsWith("PING"))
                {
                    BotDebug.Notify("\"" + irc_message + "\" recieved from the \"" + bot.name + "\" IRC connection and responded with \"PONG\"");

                    bot.connection.writer.WriteLine("PONG");
                    bot.connection.writer.Flush();
                }

                Message message = new Message(irc_message, commands, broadcaster.name);                               

                Console.WriteLine(irc_message);

                if (message.sender != default(Sender) && message.sender.name.CheckString())
                {
                    bool message_pass = spam_filter.CheckMessage(message, bot, broadcaster);

                    if (message.command != default(Command))
                    {
                        if (message.key.CheckString())
                        {
                            //TODO: change from seconds to milliseconds 
                            TimeSpan cooldown = TimeSpan.FromSeconds(message.command.cooldown),
                                     cooldown_passed = DateTime.Now - message.command.last_used;

                            if (cooldown_passed.TotalSeconds < cooldown.TotalSeconds)
                            {
                                TimeSpan cooldown_left = cooldown - cooldown_passed;

                                bot.SendWhisper(message.sender.name, $"{message.command.key} has a {cooldown.TotalSeconds.ToString("0.00")} second cooldown and can be used in {cooldown_left.TotalSeconds.ToString("0.00")} second(s)");

                                continue;
                            }

                            if(message.message_type == MessageType.Chat)
                            {
                                private_msg_queue.Enqueue(message);
                            }
                            else
                            {
                                whisper_queue.Enqueue(message);
                            }                            
                        }
                    }                 
                }
            }
        }

        /// <summary>
        /// Monitor messages coming in from the broadcaster connection.
        /// </summary>
        private void MonitorConnection_Broadcaster()
        {
            string irc_message = string.Empty;

            while (true)
            {
                irc_message = broadcaster.connection.reader.ReadLine();

                if (!broadcaster.connection.isConnected())
                {
                    BotDebug.Notify("IRC connection for \"" + broadcaster.name + " is lost. Reconnecting...");

                    broadcaster.connection.Connect();

                    continue;
                }

                if (!irc_message.CheckString())
                {
                    BotDebug.Notify("Null message recieved from the IRC connection for \"" + broadcaster.name + "\"");

                    broadcaster.connection.writer.WriteLine("PONG");
                    broadcaster.connection.writer.Flush();

                    continue;
                }

                //the irc is pinging the bot to see if it's still there, so we need to respond to it
                if (irc_message.StartsWith("PING"))
                {
                    BotDebug.Notify("\"" + irc_message + "\" recieved from the \"" + broadcaster.name + "\" IRC connection and responded with \"PONG\"");

                    broadcaster.connection.writer.WriteLine("PONG");
                    broadcaster.connection.writer.Flush();
                }
            }
        }

        /// <summary>
        /// Speak through the bot using the command line.
        /// </summary>
        private void BotSpeak()
        {
            while (true)
            {
                string input = Console.ReadLine();

                Message message = new Message(input, commands, broadcaster.name);

                bot.SendMessage(broadcaster.name, input);
            }
        }

        /*
        public void TryFollowerNotification()
        {
            //check for any followers once every 10 seconds even though the API updates once every 60 seconds
            //don't want to wait another minute if it's because of overhead in the program
            if(DateTime.Now - last_follower_check < TimeSpan.FromMilliseconds(FOLLOWER_ALERT_DELAY))
            {
                return;
            }

            last_follower_check = DateTime.Now;

            Follower[] followers;

            IEnumerable<string> difference = broadcaster.GetNewFollowers(broadcaster.name, follower_list, out followers);

            if(difference.ToArray().Length == 0)
            {
                Debug.PrintLine("No new followers");

                return;
            }

            foreach (string follower in difference)
            {
                //only announce users the first time they follow
                if (new_follower_display_names_list.Contains(follower))
                {
                    continue;
                }

                Message message = new Message();

                message.room = broadcaster.name;
                message.sender.name = bot.display_name;
                message.sender.user_type = UserType.mod;
                message.body = "Thank for for the follow " + follower + "!";

                private_msg_queue.Enqueue(message);

                new_follower_display_names_list.Add(follower);
            }

            follower_list = followers.ToList();
        }
        */

        public void TryFollowerNotification()
        {
            //check for any followers once every 10 seconds even though the API updates once every 60 seconds
            //don't want to wait another minute if it's because of overhead in the program
            if (DateTime.Now - last_follower_check < TimeSpan.FromMilliseconds(FOLLOWER_ALERT_DELAY))
            {
                return;
            }

            last_follower_check = DateTime.Now;

            IEnumerable<string> difference = broadcaster.GetNewFollowers(broadcaster.name, followers_at_launch_string, ref followers_added);

            if(difference.ToArray().Length == 0)
            {
                BotDebug.PrintLine("No new followers");

                return;
            }

            foreach(string follower in difference)
            {
                Message message = new Message();

                message.room = broadcaster.name;
                message.sender.name = bot.display_name;
                message.sender.user_type = UserType.mod;
                message.body = "Thank for for the follow, " + follower + "!";

                private_msg_queue.Enqueue(message);
            }
        }

        #endregion
    }
}
