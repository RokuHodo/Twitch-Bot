using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using TwitchBot.Chat;
using TwitchBot.Debugger;
using TwitchBot.Enums.Chat;
using TwitchBot.Extensions;
using TwitchBot.Messages;
using TwitchBot.Messages.Parser;
using TwitchBot.Models.Bot.Chat;
using TwitchBot.Models.TwitchAPI;

using TwitchBot.Helpers;

namespace TwitchBot.Clients
{
    class Bot
    {
        //PRIVATE
        readonly double WHISPER_DELAY = 400,            //using 300 is the threshhold where whispers could potentially be droped, use a higher value for safety
                        PRIVATE_MSG_DELAY = 500,        //using 300 is the absolute fasted that messages could be sent without getting globalled, use 500 in case the broadcatser want to talk as well
                        FOLLOWER_ALERT_DELAY = 5000;    //check once every 5 second in case overhead delays the notification 

        DateTime last_whisper_sent, 
                 last_private_msg_sent,                 
                 last_follower_check,
                 newest_follower_updated_at;

        Quotes quotes;
        Variables variables;
        Commands commands;
        SpamFilter spam_filter;

        Queue<TwitchMessage> whisper_queue,
                       private_msg_queue;
                
        IEnumerable<Follower> followers_at_launch_IE;

        Trie followers_at_launch_trie;

        TwitchClientOAuth bot,
                          broadcaster;

        public Bot(TwitchClientOAuth _bot, TwitchClientOAuth _broadcaster)
        {
            TwitchMessage message = new TwitchMessage();

            last_whisper_sent = DateTime.Now;
            last_private_msg_sent = DateTime.Now;
            last_follower_check = DateTime.Now;            

            bot = _bot;
            TwitchNotify.SetBot(bot);

            broadcaster = _broadcaster;

            //monitor the connection for the bot
            Thread _MonitorConnection_Bot = new Thread(new ThreadStart(MonitorConnection_Bot));
            _MonitorConnection_Bot.Start();

            Thread.Sleep(50);

            //monitor the connection for the broadcaster
            Thread _MonitorConnection_Broadcaster = new Thread(new ThreadStart(MonitorConnection_Broadcaster));
            _MonitorConnection_Broadcaster.Start();

            Thread.Sleep(50);

            //speaking through the bot from the command line
            Thread _BotSpeak = new Thread(new ThreadStart(BotSpeak));
            _BotSpeak.Start();            

            Thread.Sleep(50);

            followers_at_launch_trie = new Trie();

            quotes = new Quotes();
            variables = new Variables();
            commands = new Commands(variables);
            spam_filter = new SpamFilter();

            whisper_queue = new Queue<TwitchMessage>();
            private_msg_queue = new Queue<TwitchMessage>();
            TwitchNotify.SetQueues(ref private_msg_queue, ref whisper_queue);

            //get the list of all users following the broadcaster
            followers_at_launch_IE = broadcaster.GetFollowers_All(broadcaster.name).ToList();
            
            DebugBot.Notify("Followers");
            foreach (Follower follower in followers_at_launch_IE)
            {
                followers_at_launch_trie.Insert(follower.user.display_name);

                DebugBot.PrintLine(follower.user.display_name, follower.created_at.ToLocalTime().ToString());
            }

            Follower[] temp_follower_array = followers_at_launch_IE.ToArray();

            //JsonConvert.SerializeObject(temp_follower_array, Formatting.Indented).OverrideFile(Environment.CurrentDirectory + "/JSON/Chat/Followers Array.json");

            if(temp_follower_array.Length > 0)
            {
                //store the date of the newest follower
                newest_follower_updated_at = temp_follower_array[0].created_at;

                Thread.Sleep(50);

                //Thread _Monitor_Followers = new Thread(new ThreadStart(Monitor_Followers));
                //_Monitor_Followers.Start();
            }
        }

        #region Join and Leave a channel

        /// <summary>
        /// Join a channel to moderate.
        /// </summary>
        public void JoinChannel(string broadcaster_user_name)
        {
            DebugBot.BlankLine();
            DebugBot.Notify("Joining room: " + broadcaster_user_name.ToLower());

            bot.connection.writer.WriteLine("JOIN #" + broadcaster_user_name.ToLower());
            bot.connection.writer.Flush();
        }

        #endregion

        #region Process commands

        /// <summary>
        /// Sends a message to Twitch if there are any queued.
        /// </summary>
        public void TrySendingPrivateMessage()
        {
            if(DateTime.Now - last_private_msg_sent < TimeSpan.FromMilliseconds(PRIVATE_MSG_DELAY) || private_msg_queue.Count == 0)
            {
                return;
            }

            TwitchMessage message;

            //make sure we don't try and process a "blank" message
            do
            {
                message = private_msg_queue.Dequeue();
            }
            while (!message.body.CheckString() && private_msg_queue.Count > 0);

            //just in case the last message in the queue had a blank body
            if (!message.body.CheckString())
            {
                return;
            }

            if (message.command != default(Command))
            {
                if (CanProcessMessage(message.message_type, message))
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

        /// <summary>
        /// Sends a whisper to its respective sender if there are any queued.
        /// </summary>
        public void TrySendingWhisper()
        {
            if (DateTime.Now - last_whisper_sent < TimeSpan.FromMilliseconds(WHISPER_DELAY) || whisper_queue.Count == 0)
            {
                return;
            }

            TwitchMessage message;

            //make sure we don't try and process a "blank" message
            do
            {
                message = whisper_queue.Dequeue();
            }
            while (!message.body.CheckString() && whisper_queue.Count > 0);

            //just in case the last message in the queue had a blank body
            if(!message.body.CheckString())
            {
                return;
            }

            if (message.command != default(Command))
            {
                if (CanProcessMessage(message.message_type, message))
                {
                    ProcessCommand(MessageType.Whisper, message);
                }
            }
            else
            {
                bot.SendResponse(MessageType.Whisper, message, message.body);
            }

            last_whisper_sent = DateTime.Now;
        }

        /// <summary>
        /// Checks to make sure a user has the right permission level and is using the command in the right chat room.
        /// </summary>
        private bool CanProcessMessage(MessageType message_type, TwitchMessage message)
        {
            UserType requirement = message.command.permission;

            //make sure the user has the correct permission
            //NOTE: a user always has a UserType of "viewer" when sending a whisper can cause issues if using mod only commands through whispers
            if (!message.sender.MeetskPermissionRequirement(message.sender.user_type, requirement))
            {
                bot.SendWhisper(message.sender.name, $"You need to be a(n) {requirement.ToString()} to use {message.command.key}");

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
        private void ProcessCommand(MessageType message_type, TwitchMessage message)
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
                    bot.SendResponse(message_type, message, broadcaster.GetHowLong_String(message.sender.name, broadcaster.display_name));        
                    break;
                case "!music":
                    bot.SendResponse(message_type, message, commands.GetCurrentSong());
                    break;
                case "!setfilter":
                    spam_filter.ChangeSetting(message, commands);
                    break;
                case "!commands":
                    bot.SendResponse(message_type, message, commands.GetCommands());
                    break;
                case "!shoutout":
                    bot.SendMessage(message.room, commands.GetShoutoutMessage(message, broadcaster));
                    break;
                default:
                    bot.SendResponse(message_type, message, commands.GetCommandResponse(message.command.key, variables));
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
                    DebugBot.Warning("IRC connection for \"" + bot.name + " is lost. Reconnecting...");

                    bot.connection.Reconnect();

                    continue;
                }

                if (!irc_message.CheckString())
                {
                    DebugBot.Warning("Null message recieved from the IRC connection for \"" + bot.name + "\". Reconnecting...");

                    bot.connection.Reconnect();

                    continue;
                }

                //the irc is pinging the bot to see if it's still there, so we need to respond to it
                if (irc_message.StartsWith("PING"))
                {
                    DebugBot.Notify("\"" + irc_message + "\" recieved from the \"" + bot.name + "\" IRC connection and responded with \"PONG\"");

                    bot.connection.writer.WriteLine("PONG");
                    bot.connection.writer.Flush();
                }

                //DebugBot.PrintLine(irc_message);

                TwitchMessage message = MessageParser.Parse(commands, irc_message, broadcaster.display_name);

                //check to see if anyting is "blank"
                if (message == default(TwitchMessage) || message.sender == default(Sender))
                {
                    continue;
                }

                //only enqueue the message if it has something to print and that someone actually sent it
                if (!message.body.CheckString() || !message.sender.name.CheckString())
                {
                    continue;
                }

                message = message.CheckForNotification(message, bot);

                if(message.command == default(Command) && !message.is_notification)
                {
                    continue;
                }
                
                //skip the message if it contains spam
                if (spam_filter.ContainsSpam(message, bot, broadcaster))
                {
                    continue;
                }                

                //check to see if a command is being used in the cooldown period
                if (message.command != default(Command) && message.command.key.CheckString())
                {
                    TimeSpan cooldown = TimeSpan.FromMilliseconds(message.command.cooldown),
                             cooldown_passed = DateTime.Now - message.command.last_used;

                    if (cooldown_passed.TotalMilliseconds < cooldown.TotalMilliseconds)
                    {
                        TimeSpan cooldown_left = cooldown - cooldown_passed;

                        bot.SendWhisper(message.sender.name, message.command.key + " has a " + cooldown.TotalSeconds.ToString("0.00") + " second cooldown and can be used in " + cooldown_left.TotalSeconds.ToString("0.00") + " second(s)" );

                        continue;
                    }
                }                

                //everything checks out, enqueue the messahe to be printed
                if (message.message_type == MessageType.Chat)
                {
                    private_msg_queue.Enqueue(message);
                }
                else
                {
                    whisper_queue.Enqueue(message);
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
                    DebugBot.Warning("IRC connection for \"" + broadcaster.name + " is lost. Reconnecting...");

                    broadcaster.connection.Reconnect();

                    continue;
                }

                if (!irc_message.CheckString())
                {
                    DebugBot.Warning("Null message recieved from the IRC connection for \"" + broadcaster.name + "\". Reconnecting...");

                    broadcaster.connection.Reconnect();

                    continue;
                }

                //the irc is pinging the bot to see if it's still there, so we need to respond to it
                if (irc_message.StartsWith("PING"))
                {
                    DebugBot.Notify("\"" + irc_message + "\" recieved from the \"" + broadcaster.name + "\" IRC connection and responded with \"PONG\"");

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

                TwitchMessage message = new TwitchMessage
                {
                    body = input,

                    sender = new Sender
                    {
                        name = bot.display_name,
                        user_type = UserType.mod
                    }
                };

                bot.SendMessage(broadcaster.name, input);
            }
        }

        /// <summary>
        /// Checks for new followers and announces them.
        /// </summary>
        private void Monitor_Followers()
        {
            while (true)
            {
                //check for any followers once every 10 seconds even though the API updates once every 60 seconds
                if (DateTime.Now - last_follower_check < TimeSpan.FromMilliseconds(FOLLOWER_ALERT_DELAY))
                {
                    Thread.Sleep(50);

                    continue;
                }

                last_follower_check = DateTime.Now;

                IEnumerable<string> difference = broadcaster.GetNewFollowers(broadcaster.name, ref newest_follower_updated_at, ref followers_at_launch_trie);

                if (difference.ToArray().Length == 0)
                {
                    //DebugBot.PrintLine("No new followersa found", ConsoleColor.Green);

                    Thread.Sleep(50);

                    continue;
                }

                foreach (string follower in difference)
                {
                    Sender _sender = new Sender
                    {
                        name = bot.name,
                        user_type = UserType.mod
                    };

                    TwitchMessage message = new TwitchMessage
                    {
                        room = broadcaster.name,
                        body = "Thank you for the follow, " + follower + "!",
                        sender = _sender,

                        message_type = MessageType.Chat,
                        command = default(Command)
                    };

                    private_msg_queue.Enqueue(message);
                }

                Thread.Sleep(50);
            }            
        }

        #endregion

    }
}
