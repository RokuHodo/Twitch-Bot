using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using TwitchChatBot.Clients;
using TwitchChatBot.Debugger;
using TwitchChatBot.Extensions;

/*
    TODO(Six):
        - Add in the check to see which cluster to connect to
        - Add in the ability to whisper through the command line
        - Debug commands could be more descriptive, but they get the job done for now
        - Make it so that variable values can contain other variables? This seems like it would be a very specific use case.
          I'm not sure it's worth the time to implement 
*/

namespace TwitchChatBot
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Twitch Chat Bot";

            string file_path = Environment.CurrentDirectory + "\\Login.txt";            

            string[] login = Load(File.ReadAllLines(file_path));

            if(login == null || login.Length != 3)
            {
                Debug.Failed($"Failed to load the login information: {login.Length} login credentials found, 3 credentials are required");
                Debug.SubText("Bot token, broadcaster token, and a client id");

                Console.WriteLine(Environment.NewLine + "Press any key to exit...");
                Console.ReadKey();

                Environment.Exit(0);
            }

            TwitchUserAuthenticated broadcaster = new TwitchUserAuthenticated(login[2], login[1]);

            if (!broadcaster.display_name.CheckString())
            {
                Debug.Failed("Failed find the broadcaster");

                Console.WriteLine(Environment.NewLine + "Press any key to exit...");
                Console.ReadKey();

                Environment.Exit(0);
            }   
                        
            TwitchBot bot = new TwitchBot(login[0], login[1], broadcaster);
            bot.JoinChannel(broadcaster.GetAuthenticatedUser().name);

            while (true)
            {
                if (!bot.ChatConnected())
                {
                    Debug.Notify("Attempting to recconect to chat...");

                    bot.ConnectChat();
                }

                if (!bot.WhisperConnected())
                {                    
                    Debug.Notify("Attempting to recconect to the whisper server...");
                    
                    bot.ConnectWhisper();
                }

                bot.TryProcessCommand(bot);

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
