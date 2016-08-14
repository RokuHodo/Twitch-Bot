using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using TwitchChatBot.Clients;
using TwitchChatBot.Debugger;
using TwitchChatBot.Extensions;

using TwitchChatBot.Models.TwitchAPI;
using System.Linq;

/*
    TODO: - Make quotes non-zero based?
          - Have a better !commands command
          - Make sure all debug and Notify messages are in place
          - Keep testing follower notificaiton
          - Put in host notifications

*/

namespace TwitchChatBot
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Twitch Chat Bot";

            string file_path = Environment.CurrentDirectory + "/Login.txt";            

            string[] login = Load(File.ReadAllLines(file_path));

            if(login == null || login.Length != 3)
            {
                BotDebug.Error($"Failed to load the login information: {login.Length} login credentials found, 3 credentials are required");
                BotDebug.PrintLine("Bot token, broadcaster token, and a client id");

                BotDebug.BlankLine();
                BotDebug.PrintLine("Press any key to exit...");
                Console.ReadKey();

                Environment.Exit(0);
            }

            TwitchClientOAuth broadcaster = new TwitchClientOAuth(login[2], login[1]);

            if (!broadcaster.display_name.CheckString())
            {
                BotDebug.Error("Failed to find the broadcaster");
                BotDebug.BlankLine();

                BotDebug.PrintLine("Press any key to exit...");
                Console.ReadKey();

                Environment.Exit(0);
            }   
                        
            Bot bot = new Bot(login[0], login[1], broadcaster);
            bot.JoinChannel(broadcaster.GetAuthenticatedUser().name);

            while (true)
            {
                bot.TrySendingWhisper();
                bot.TryFollowerNotification();                

                bot.TrySendingPrivateMessage();

                Thread.Sleep(100);
            }
        }       

        private static string[] Load(string[] login_file)
        {
            List<string> login_info = new List<string>();

            foreach (string line in login_file)
            {
                if (line.CheckString() && !line.StartsWith("//"))
                {
                    login_info.Add(line);
                }                
            }

            return login_info.ToArray();
        }
    }    
}
