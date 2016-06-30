using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

using TwitchChatBot.Enums.Debug;
using TwitchChatBot.Clients;
using TwitchChatBot.Debugger;
using TwitchChatBot.Extensions;
using TwitchChatBot.Extensions.Files;
using TwitchChatBot.Models.Bot;

namespace TwitchChatBot.Chat
{
    class Quotes
    {
        List<Quote> quotes_list = new List<Quote>();

        string file_path = Environment.CurrentDirectory + "/JSON/Chat/Quotes.json";

        public Quotes()
        {
            string quotes_preloaded;

            List<Quote> quotes_preloaded_list;

            Debug.BlankLine();

            Debug.BlockBegin();
            Debug.Header("Loading Quotes");
            Debug.PrintLine("File path:", file_path);

            quotes_preloaded = File.ReadAllText(file_path);
            quotes_preloaded_list = JsonConvert.DeserializeObject<List<Quote>>(quotes_preloaded);

            if (quotes_preloaded_list != null)
            {
                foreach (Quote quote in quotes_preloaded_list)
                {
                    Load(quote);
                }
            }

            Debug.BlockEnd();
        }

        /// <summary>
        /// Loads a <see cref="Quote"/> into the <see cref="quotes_list"/>
        /// </summary>
        /// <param name="command">The command to load.</param>
        private void Load(Quote quote)
        {
            Debug.BlankLine();
            Debug.SubHeader("Loading quote...");

            if (!CheckSyntax(DebugMethod.Load, quote) || Exists(DebugMethod.Load, quote))
            {               
                return;
            }

            try
            {
                quotes_list.Add(quote);

                Debug.Success(DebugMethod.Load, DebugObject.Quote, quote.quote);
                Debug.PrintObject(quote);
            }
            catch (Exception exception)
            {
                Debug.Error(DebugMethod.Load, DebugObject.Quote, DebugError.Exception);
                Debug.PrintLine(nameof(quote.quote), quote.quote);
                Debug.PrintLine(nameof(exception), exception.Message);

                return;
            }
        }

        /// <summary>
        /// Searches the Twitch chat message (<see cref="Message.body"/>) for a command and response to then be added.
        /// Called by the user from Twitch chat by using the "!addcommand" command.
        /// </summary>
        /// <param name="commands">Used to parse the message for the quote.</param>        
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="broadcaster">Coontains the broadcaster name to be appended to the end of the quote</param>
        public void Add(Commands commands, Message message, TwitchClientOAuth broadcaster)
        {
            Debug.BlankLine();
            Debug.SubHeader("Adding quote...");

            Quote quote = MessageToQuote(DebugMethod.Add, commands, message, broadcaster);

            //check to see if the quote is empty
            if (!CheckSyntax(DebugMethod.Add, quote))
            {
                Notify.Failed(DebugMethod.Add, DebugObject.Quote, quote.quote, DebugError.Syntax, message);

                return;
            }

            //check to see if the same quote already exists
            if (Exists(DebugMethod.Add, quote))
            {
                Notify.Failed(DebugMethod.Add, DebugObject.Quote, quote.quote, DebugError.ExistYes, message);

                return;
            }

            try
            {
                quotes_list.Add(quote);

                JsonConvert.SerializeObject(quotes_list, Formatting.Indented).OverrideFile(file_path);

                Notify.Success(DebugMethod.Add, DebugObject.Quote, quote.quote, message);

                Debug.Success(DebugMethod.Load, DebugObject.Quote, quote.quote);
                Debug.PrintObject(quote);
            }
            catch (Exception exception)
            {
                Notify.Failed(DebugMethod.Add, DebugObject.Quote, quote.quote, DebugError.Exception, message);

                //something went wrong
                Debug.Error(DebugMethod.Load, DebugObject.Quote, DebugError.Exception);
                Debug.PrintLine(nameof(quote.quote), quote.quote);
                Debug.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Gets a random quote from the list.
        /// </summary>
        /// <returns></returns>
        public string GetQuote()
        {
            if(quotes_list.Count < 1)
            {
                return "There are no quotes yet!";
            }

            Random random = new Random();

            Quote quote = quotes_list[random.Next(quotes_list.Count)];

            return quote.quote + " - " + quote.quotee + " " + quote.date;
        }

        /// <summary>
        /// Checks to see if the quote is enclosed in quotes.
        /// If the text is not enclosed in quotes, they are added.
        /// </summary>
        /// <param name="quote">The quote to check.</param>
        /// <param name="broadcaster">Coontains the broadcaster name to be appended to the end of the quote.</param>
        /// <returns></returns>
        private string ValidateSyntax(string quote, TwitchClientOAuth broadcaster)
        {
            int index = quote.LastIndexOf($" - {broadcaster.display_name}");

            string suffix;

            //custom quote loaded from file, return it as is
            if(index == -1)
            {
                return quote;
            }

            suffix = quote.Substring(index);
            quote = RemoveSuffix(quote, broadcaster);            

            if (!quote.StartsWith("\""))
            {
                quote = "\"" + quote;
            }

            if (!quote.EndsWith("\""))
            {
                quote += "\"";
            }

            return quote + suffix;
        }

        /// <summary>
        /// Removes the name of the broadcaster and the any quotations.
        /// Used when checking to see if the quote to add is null or empty.
        /// </summary>
        /// <param name="quote">The quote to parse.</param>
        /// <param name="broadcaster">Coontains the broadcaster name to be removed from the quote.</param>
        /// <returns></returns>
        private string RemoveSuffix(string quote, TwitchClientOAuth broadcaster)
        {
            quote = quote.Replace("\"", "");

            int index = quote.LastIndexOf($" - {broadcaster.display_name}");            

            if(index != -1)
            {
                quote = quote.Substring(0, index);
            }

            return quote;
        }

        /// <summary>
        /// Checks to see if the quote matches the proper syntax.
        /// </summary>
        /// <param name="quote">The quote to parse.</param>
        /// <returns></returns>
        private bool CheckSyntax(DebugMethod debug_method, Quote quote)
        {
            string _quote = quote.quote.Replace("\"", string.Empty);

            if (!_quote.CheckString())
            {
                Debug.SyntaxError(DebugObject.Quote, DebugObject.Quote, SyntaxError.Null);
                Debug.Error(debug_method, DebugObject.Quote, DebugError.Syntax);
                Debug.PrintLine(nameof(quote.quote), "null");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks to see if a quote already exists.
        /// </summary>
        /// <param name="quote">Quote to check.</param>
        /// <returns></returns>
        private bool Exists(DebugMethod debug_method, Quote quote)
        {
            if(quotes_list.Exists(x => x.quote == quote.quote))
            {
                Debug.Error(debug_method, DebugObject.Quote, DebugError.ExistYes);
                Debug.PrintLine(nameof(quote.quote), quote.quote);

                return true;
            }

            return false;
        }

        private Quote MessageToQuote(DebugMethod debug_method, Commands commands, Message message, TwitchClientOAuth broadcaster)
        {
            string quote_string = string.Empty;

            quote_string = commands.ParseCommandString(message);

            try
            {
                Quote quote = new Quote
                {
                    quote = quote_string,
                    quotee = broadcaster.display_name,
                    date = DateTime.Now                    
                };

                Debug.Success(DebugMethod.Serialize, DebugObject.Quote, quote.quote);
                Debug.PrintObject(quote);

                return quote;
            }
            catch (Exception exception)
            {
                Notify.Failed(debug_method, DebugObject.Quote, quote_string, DebugError.Exception, message);

                Debug.Error(DebugMethod.Serialize, DebugObject.Quote, DebugError.Exception);
                Debug.Error(debug_method, DebugObject.Quote, DebugError.Null);
                Debug.PrintLine(nameof(quote_string), quote_string);
                Debug.PrintLine(nameof(exception), exception.Message);

                return null;
            }
        }
    }
}
