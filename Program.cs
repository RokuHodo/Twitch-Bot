using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using TwitchChatBot.Clients;
using TwitchChatBot.Debugger;
using TwitchChatBot.Extensions;

using TwitchChatBot.Chat;

/*
    TODO(Six):

        - Add in the ability to whisper through the command line
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
                Debug.Error($"Failed to load the login information: {login.Length} login credentials found, 3 credentials are required");
                Debug.PrintLine("Bot token, broadcaster token, and a client id");

                Console.WriteLine(Environment.NewLine + "Press any key to exit...");
                Console.ReadKey();

                Environment.Exit(0);
            }

            TwitchClientOAuth broadcaster = new TwitchClientOAuth(login[2], login[1]);

            if (!broadcaster.display_name.CheckString())
            {
                Debug.Error("Failed to find the broadcaster");
                Debug.BlankLine();

                Debug.PrintLine("Press any key to exit...");
                Console.ReadKey();

                Environment.Exit(0);
            }   
                        
            Bot bot = new Bot(login[0], login[1], broadcaster);
            bot.JoinChannel(broadcaster.GetAuthenticatedUser().name);

            while (true)
            {
                bot.TryProcessCommand();

                Thread.Sleep(100);
            }
        }       


        private static string[] Load(string[] login_file)
        {
            List<string> login_info = new List<string>();

            //remove any comments or blank lines
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
