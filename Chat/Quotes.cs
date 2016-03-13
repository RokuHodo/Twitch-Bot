using System;
using System.Collections.Generic;
using System.IO;

using TwitchChatBot.Enums.Debugger;
using TwitchChatBot.Clients;
using TwitchChatBot.Debugger;
using TwitchChatBot.Extensions;
using TwitchChatBot.Extensions.Files;

namespace TwitchChatBot.Chat
{
    class Quotes
    {
        List<string> quotes = new List<string>();

        string[] preloaded_quotes;

        string file_path = Environment.CurrentDirectory + "/Quotes.txt";

        public Quotes(TwitchUserAuthenticated broadcaster)
        {
            Debug.Header("Preloading quotes");
            Debug.SubText("File path: " + file_path);

            preloaded_quotes = PreLoad(File.ReadAllLines(file_path));

            Debug.BlankLine();
            Debug.Header("Loading quotes");
            Debug.BlankLine();

            foreach (string line in preloaded_quotes)
            {
                Load(line, broadcaster);
            }
        }

        /// <summary>
        /// Loops through an array of strings and returns the elements that contain more than one word into a <see cref="List{T}"/> on launch.
        /// Commented lines and whitespace lines are ignored. 
        /// </summary>
        /// <param name="lines">Array of strings to be processed.</param>
        /// <returns></returns>
        private string[] PreLoad(string[] lines)
        {
            List<string> preloaded_quotes = new List<string>();

            foreach(string line in lines)
            {
                if (line.CheckString() && !line.StartsWith("//"))
                {
                    Debug.SubHeader(" Preloading quote...");            

                    try
                    {                       
                        preloaded_quotes.Add(line);

                        Debug.Success(DebugMethod.PreLoad, DebugObject.Quote, line);
                    }
                    catch (Exception ex)
                    {
                        Debug.Failed(DebugMethod.PreLoad, DebugObject.Quote, DebugError.Exception);
                        Debug.SubText("Quote: " + line);
                        Debug.SubText("Exception: " + ex.Message);
                    }                    
                }
            }            

            return preloaded_quotes.ToArray();
        }

        /// <summary>
        /// Loads a quote into the <see cref="quotes"/> <see cref="List{T}"/> on launch.
        /// </summary>
        /// <param name="quote">Quote to add.</param>
        /// <param name="broadcaster">Coontains the broadcaster name to be appended to the end of the quote.</param>
        /// <param name="message">(Optional parameter) Required to send a chat message or whisper by calling <see cref="Notify"/>.Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">(Optional parameter) Required to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        /// <returns></returns>
        private bool Load(string quote, TwitchUserAuthenticated broadcaster, Message message = null, TwitchBot bot = null)
        {
            bool send_response = message != null && bot != null;

            string tag = "quote";

            Debug.SubHeader(" Loading quote...");

            //check to see if the quote is empty
            if (!CheckSyntax(quote, broadcaster))
            {
                if (send_response)
                {
                    Notify.Failed(bot, DebugMethod.Add, DebugError.Syntax, message, tag);
                }                

                Debug.Failed(DebugMethod.Load, DebugObject.Quote, DebugError.Null);
                Debug.SubText("Quote: null");

                return false;
            }

            //check to see if the same quote already exists
            if (Exists(quote))
            {
                if (send_response)
                {
                    Notify.Failed(bot, DebugMethod.Add, DebugError.ExistYes, message, tag);
                }

                Debug.Failed(DebugMethod.Load, DebugObject.Quote, DebugError.ExistYes);
                Debug.SubText("Quote: " + quote);

                return false;
            }            

            try
            {
                //add the quote!
                quote = ValidateSyntax(quote, broadcaster);

                quotes.Add(quote);

                if (send_response)
                {
                    Notify.Success(bot, DebugMethod.Add, message, tag);
                }

                Debug.Success(DebugMethod.Load, DebugObject.Quote, quote);
                Debug.SubText("Quote: " + quote);
            }
            catch (Exception ex)
            {
                if (send_response)
                {
                    Notify.Failed(bot, DebugMethod.Add, DebugError.Exception, message, tag);
                }

                //something went wrong
                Debug.Failed(DebugMethod.Load, DebugObject.Quote, DebugError.Exception);
                Debug.SubText("Quote: " + quote);
                Debug.SubText("Exception: " + ex.Message);

                return false;
            }

            return true;            
        }

        /// <summary>
        /// Searches the Twitch chat message (<see cref="Message.body"/>) for a command and response to then be added.
        /// Called by the user from Twitch chat by using the "!addcommand" command.
        /// </summary>
        /// <param name="commands">Used to parse the message for the quote.</param>        
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">Used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        /// <param name="broadcaster">Coontains the broadcaster name to be appended to the end of the quote</param>
        public void Add(Commands commands, Message message, TwitchBot bot, TwitchUserAuthenticated broadcaster)
        {
            Debug.SubHeader(" Adding quote...");

            string quote = commands.ParseCommandString(message);            

            quote += $" - {broadcaster.display_name} {DateTime.Now.ToShortDateString()}";
                       
            if(Load(quote, broadcaster, message, bot))
            {
                //need to revalidate the quote so the proper quote is added to the text file
                //could probably just use "out" but I'm too lazy to change it at this point
                quote = ValidateSyntax(quote, broadcaster);

                quote.AppendToFile(file_path);
            }
        }

        /// <summary>
        /// Gets a random quote from the list.
        /// </summary>
        /// <returns></returns>
        public string GetQuote()
        {
            if(quotes.Count < 1)
            {
                return "There are no quotes yet!";
            }

            Random random = new Random();

            return quotes[random.Next(quotes.Count)];
        }

        /// <summary>
        /// Checks to see if the quote is enclosed in quotes.
        /// If the text is not enclosed in quotes, they are added.
        /// </summary>
        /// <param name="quote">The quote to check.</param>
        /// <param name="broadcaster">Coontains the broadcaster name to be appended to the end of the quote.</param>
        /// <returns></returns>
        private string ValidateSyntax(string quote, TwitchUserAuthenticated broadcaster)
        {
            int index = quote.LastIndexOf($" - {broadcaster.display_name}");

            string _quote = RemoveSuffix(quote, broadcaster);

            if (!_quote.StartsWith("\""))
            {
                _quote = "\"" + _quote;
            }

            if (!_quote.EndsWith("\""))
            {
                _quote += "\"";
            }

            return _quote + quote.Substring(index);
        }

        /// <summary>
        /// Removes the name of the broadcaster and the any quotations.
        /// Used when checking to see if the quote to add is null or empty.
        /// </summary>
        /// <param name="quote">The quote to parse.</param>
        /// <param name="broadcaster">Coontains the broadcaster name to be removed from the quote.</param>
        /// <returns></returns>
        private string RemoveSuffix(string quote, TwitchUserAuthenticated broadcaster)
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
        private bool CheckSyntax(string quote, TwitchUserAuthenticated broadcaster)
        {
            quote = RemoveSuffix(quote, broadcaster);

            if (!quote.CheckString())
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Quote, DebugObject.Quote, SyntaxError.Null);
                Debug.SubText("Quote: null");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks to see if a quote already exists.
        /// </summary>
        /// <param name="quote">Quote to check.</param>
        /// <returns></returns>
        private bool Exists(string quote)
        {
            return quotes.Exists(x => x == quote);
        }
    }
}
