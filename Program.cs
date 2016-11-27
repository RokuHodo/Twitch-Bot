using System;
using System.IO;
using System.Threading;

using Newtonsoft.Json;

using TwitchBot.Clients;
using TwitchBot.Debugger;
using TwitchBot.Extensions;
using TwitchBot.Models.Bot;

/* --------------------------------------------------------

    TODO: 
          - Separate utility commands (!command, !variable, !quote, !add, !edit, and !remove) from user commands using reflection
          - Make the utility commands readonly, i.e - cannot be removed or edited, even by the broadcaster
          - Make !add, !edit, and !remove actual commands instead of being hard coded (update ExtractCommand as needed)
          - Remove all unnecessary commenting and update summaries

-------------------------------------------------------- */

namespace TwitchBot
{
    class Program
    {       
        static void Main()
        {            
            string LOGIN_PATH = Environment.CurrentDirectory + "/JSON/Login.json";

            Console.Title = "Twitch Bot (...)";            

            string login_error = string.Empty,
                   login_preloaded = File.ReadAllText(LOGIN_PATH);

            Login login = JsonConvert.DeserializeObject<Login>(login_preloaded);

            if (!CheckLogin_Credentials(login, out login_error))
            {
                DebugBot.PrintLine(login_error);

                CloseBot();
            }            

            TwitchClientOAuth bot_client = new TwitchClientOAuth(login.client_id, login.bot_token),
                              broadcaster_client = new TwitchClientOAuth(login.client_id, login.broadcaster_token);

            if (!CheckLogin_Clients(bot_client, broadcaster_client, out login_error))
            {
                DebugBot.PrintLine(login_error);

                CloseBot();
            }

            Console.Title = "Twitch Bot (" + bot_client.display_name + ")";

            Bot bot = new Bot(bot_client, broadcaster_client);
            bot.JoinChannel(broadcaster_client.GetAuthenticatedUser().name);

            while (true)
            {
                bot.TrySendingWhisper();          
                bot.TrySendingPrivateMessage();

                Thread.Sleep(100);
            }
        }

        private static bool CheckLogin_Credentials(Login login, out string error)
        {
            error = string.Empty;

            if (!login.client_id.CheckString())
            {
                error = "Client ID could not be found";

                return false;
            }

            if (!login.bot_token.CheckString())
            {
                error = "The oauth token for the bot could not be found";

                return false;
            }

            if (!login.broadcaster_token.CheckString())
            {
                error = "The oauth token for the broadcaster could not be found";

                return false;
            }

            return true;
        }

        private static bool CheckLogin_Clients(TwitchClientOAuth bot, TwitchClientOAuth broadcaster, out string error)
        {
            error = string.Empty;

            if (!bot.display_name.CheckString())
            {
                error = "The bot client could not be found or does not exist";

                return false;
            }

            if (!broadcaster.display_name.CheckString())
            {
                error = "The broadcaster client could not be found or does not exist";

                return false;
            }

            return true;
        }

        private static void CloseBot()
        {
            DebugBot.PrintLine("Press any key to exit...");

            Console.ReadKey();

            Environment.Exit(0);
        }
    }    
}
