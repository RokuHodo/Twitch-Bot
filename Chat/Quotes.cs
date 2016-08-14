using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

using TwitchChatBot.Enums.Debug;
using TwitchChatBot.Clients;
using TwitchChatBot.Debugger;
using TwitchChatBot.Enums.Extensions;
using TwitchChatBot.Extensions;
using TwitchChatBot.Extensions.Files;
using TwitchChatBot.Models.Bot;

namespace TwitchChatBot.Chat
{
    class Quotes
    {
        readonly string FILE_PATH = Environment.CurrentDirectory + "/JSON/Chat/Quotes.json";

        List<Quote> quotes;        

        public Quotes()
        {
            string quotes_preloaded;

            quotes = new List<Quote>();

            List<Quote> quotes_preloaded_list;

            BotDebug.BlankLine();

            BotDebug.BlockBegin();
            BotDebug.Header("Loading Quotes");
            BotDebug.PrintLine("File path:", FILE_PATH);

            quotes_preloaded = File.ReadAllText(FILE_PATH);
            quotes_preloaded_list = JsonConvert.DeserializeObject<List<Quote>>(quotes_preloaded);

            if (quotes_preloaded_list != null)
            {
                foreach (Quote quote in quotes_preloaded_list)
                {
                    Load(quote);
                }
            }

            BotDebug.BlockEnd();
        }

        #region Load quotes

        /// <summary>
        /// Loads a <see cref="Quote"/> into the <see cref="quotes"/> list to be called in real time.
        /// </summary>
        /// <param name="command">The command to load.</param>
        private void Load(Quote quote)
        {
            BotDebug.BlankLine();
            BotDebug.SubHeader("Loading quote...");

            if (!CheckSyntax(DebugMethod.Load, quote) || Exists(DebugMethod.Load, quote))
            {               
                return;
            }

            try
            {
                quotes.Add(quote);

                BotDebug.Success(DebugMethod.Load, DebugObject.Quote, quote.quote);
                BotDebug.PrintObject(quote);
            }
            catch (Exception exception)
            {
                BotDebug.Error(DebugMethod.Load, DebugObject.Quote, DebugError.Exception);
                BotDebug.PrintLine(nameof(quote.quote), quote.quote);
                BotDebug.PrintLine(nameof(exception), exception.Message);

                return;
            }
        }

        #endregion

        #region Add quotes

        /// <summary>
        /// Modify the variables by adding, editting, or removing.
        /// </summary>
        /// <param name="commands">Used for parsing the body.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        public void Modify(Commands commands, Message message, TwitchClientOAuth broadcaster, TwitchClientOAuth bot)
        {
            string temp = commands.ParseCommandString(message),
                   key = temp.TextBefore(" ");

            message.body = temp.TextAfter(" ").CheckString() ? temp.TextAfter(" ") : temp;

            try
            {
                switch (key)
                {
                    case "!add":
                        Add(message, broadcaster);
                        break;
                    case "!edit":
                        Edit(message, broadcaster);
                        break;
                    case "!remove":
                        Remove(message);
                        break;
                    default:
                        bot.SendMessage(message.room, GetQuote(message));
                        break;
                }
            }
            catch (Exception exception)
            {
                BotDebug.Error(DebugMethod.Modify, DebugObject.Quote, DebugError.Exception);
                BotDebug.PrintLine(nameof(exception), exception.Message);
                BotDebug.PrintLine(nameof(temp), temp);
                BotDebug.PrintLine(nameof(key), key);
                BotDebug.PrintLine(nameof(message.body), message.body);
            }
        }

        /// <summary>
        /// Parses the <see cref="Message.body"/> of a message and adds a <see cref="Quote"/> and adds it to the <see cref="quotes"/> list to be called in real time.
        /// Called by the user from Twitch chat by using the "!addquote" command.
        /// </summary>
        /// <param name="commands">Used to parse the message for the quote.</param>        
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="broadcaster">Coontains the broadcaster name to be appended to the end of the quote</param>
        public void Add(Message message, TwitchClientOAuth broadcaster)
        {
            BotDebug.BlankLine();
            BotDebug.SubHeader("Adding quote...");

            Quote quote = MessageToQuote(DebugMethod.Add, message, broadcaster);

            if (!CheckSyntax(DebugMethod.Add, quote))
            {
                Notify.Error(DebugMethod.Add, DebugObject.Quote, quote.quote, DebugError.Syntax, message);

                return;
            }

            if (Exists(DebugMethod.Add, quote))
            {
                Notify.Error(DebugMethod.Add, DebugObject.Quote, quote.quote, DebugError.ExistYes, message);

                return;
            }

            try
            {
                quotes.Add(quote);

                JsonConvert.SerializeObject(quotes, Formatting.Indented).OverrideFile(FILE_PATH);

                Notify.Success(DebugMethod.Add, DebugObject.Quote, quote.quote, message);

                BotDebug.Success(DebugMethod.Add, DebugObject.Quote, quote.quote);
                BotDebug.PrintObject(quote);
            }
            catch (Exception exception)
            {
                Notify.Error(DebugMethod.Add, DebugObject.Quote, quote.quote, DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Add, DebugObject.Quote, DebugError.Exception);
                BotDebug.PrintLine(nameof(quote.quote), quote.quote);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }
        }

        public void Edit(Message message, TwitchClientOAuth broadcaster)
        {
            BotDebug.BlankLine();
            BotDebug.SubHeader("Editing quote...");                        

            if (!message.body.CheckString())
            {
                Notify.Error(DebugMethod.Edit, DebugObject.Quote, message.body, DebugError.Null, message);

                BotDebug.Error(DebugMethod.Edit, DebugObject.Quote, DebugError.Null);
                BotDebug.PrintLine(nameof(message.body), "null");

                return;
            }

            int index = GetIndex(message);

            if(index == -1)
            {
                Notify.Error(DebugMethod.Edit, DebugObject.Quote, message.body, DebugError.Bounds, message);

                BotDebug.Error(DebugMethod.Edit, DebugObject.Quote, DebugError.Bounds);
                BotDebug.PrintLine(nameof(index), index.ToString());
                BotDebug.PrintLine(nameof(quotes.Count), quotes.Count.ToString());

                return;
            }

            message.body = message.body.TextAfter(" ").RemoveWhiteSpace(WhiteSpace.Left);

            Quote quote = MessageToQuote(DebugMethod.Edit, message, broadcaster);
            
            if (!CheckSyntax(DebugMethod.Edit, quote))
            {
                Notify.Error(DebugMethod.Edit, DebugObject.Quote, message.body, DebugError.Syntax, message);

                return;
            }

            try
            {
                quotes[index] = quote;

                JsonConvert.SerializeObject(quotes, Formatting.Indented).OverrideFile(FILE_PATH);

                Notify.Success(DebugMethod.Edit, DebugObject.Quote, quote.quote, message);

                BotDebug.Success(DebugMethod.Edit, DebugObject.Quote, quote.quote);
                BotDebug.PrintLine(nameof(index), index.ToString());
                BotDebug.PrintObject(quote);
            }
            catch (Exception exception)
            {
                Notify.Error(DebugMethod.Edit, DebugObject.Quote, message.body, DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Edit, DebugObject.Quote, DebugError.Exception);
                BotDebug.PrintLine(nameof(quote.quote), quote.quote);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }
        }

        private void Remove(Message message)
        {
            BotDebug.BlankLine();
            BotDebug.SubHeader("Removing quote...");

            if (!message.body.CheckString())
            {
                Notify.Error(DebugMethod.Remove, DebugObject.Quote, message.body, DebugError.Null, message);

                BotDebug.Error(DebugMethod.Remove, DebugObject.Quote, DebugError.Null);
                BotDebug.PrintLine(nameof(message.body), "null");

                return;
            }

            int index = GetIndex(message);

            if (index == -1)
            {
                Notify.Error(DebugMethod.Remove, DebugObject.Quote, message.body, DebugError.Bounds, message);

                BotDebug.Error(DebugMethod.Remove, DebugObject.Quote, DebugError.Bounds);
                BotDebug.PrintLine(nameof(index), index.ToString());
                BotDebug.PrintLine(nameof(quotes.Count), quotes.Count.ToString());

                return;
            }

            try
            {
                quotes.RemoveAt(index);

                JsonConvert.SerializeObject(quotes, Formatting.Indented).OverrideFile(FILE_PATH);

                Notify.Success(DebugMethod.Remove, DebugObject.Quote, index.ToString(), message);

                BotDebug.Success(DebugMethod.Remove, DebugObject.Quote, index.ToString());
                BotDebug.PrintLine(nameof(index), index.ToString());
            }
            catch (Exception exception)
            {
                Notify.Error(DebugMethod.Remove, DebugObject.Quote, message.body, DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Remove, DebugObject.Quote, DebugError.Exception);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }
        }

        #endregion

        #region Get quote information

        /// <summary>
        /// Gets a random <see cref="Quote"/> from the <see cref="quotes"/> list.
        /// </summary>
        /// <returns></returns>
        public string GetQuote(Message message)
        {
            int index;

            string _quote = string.Empty;

            if(quotes.Count < 1)
            {
                Notify.SendMessage(message, "There are no quotes yet!");

                BotDebug.Error(DebugMethod.Retrieve, DebugObject.Quote, DebugError.Null);
                BotDebug.PrintLine(nameof(quotes.Count), quotes.Count.ToString());

                return _quote;
            }

            if (message.body.CheckString())
            {
                index = GetIndex(message);

                if (index == -1)
                {
                    Notify.Error(DebugMethod.Retrieve, DebugObject.Quote, index.ToString(), DebugError.Bounds, message);

                    BotDebug.Error(DebugMethod.Retrieve, DebugObject.Quote, DebugError.Bounds);
                    BotDebug.PrintLine(nameof(index), index.ToString());
                    BotDebug.PrintLine(nameof(quotes.Count), quotes.Count.ToString());

                    return _quote;
                }
            }
            else
            {
                index = new Random().Next(quotes.Count);
            }            

            _quote = quotes[index].quote + " - " + quotes[index].quotee + " " + quotes[index].date;

            return _quote;
        }

        public string GetTotalQuotes()
        {
            string result = string.Empty;

            if(quotes.Count == 0)
            {
                result = "There are no quotes yet!";
            }
            else
            {
                if (quotes.Count == 1)
                {
                    result = "There is " + quotes.Count + " quote!";
                }
                else
                {
                    result = "There are " + quotes.Count + " quotes!";
                }

                result += " To call a quote, type \"!quote\" for a random quote or \"!quote <index>\" for a specific quote. Note: <index> is zero based.";
            }            

            return result;
        }

        #endregion

        #region Boolean checks

        /// <summary>
        /// Checks to see if the quote matches the proper syntax.
        /// </summary>
        /// <param name="method">The type of operation being performed.</param>
        /// <param name="quote">The quote to parse.</param>
        /// <returns></returns>
        private bool CheckSyntax(DebugMethod method, Quote quote)
        {
            string _quote = quote.quote.Replace("\"", string.Empty);

            if (!_quote.CheckString() || quote == default(Quote))
            {              
                BotDebug.SyntaxError(DebugObject.Quote, DebugObject.Quote, SyntaxError.Null);
                BotDebug.Error(method, DebugObject.Quote, DebugError.Syntax);
                BotDebug.PrintLine(nameof(quote.quote), "null");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks to see if a quote already exists.
        /// </summary>
        /// /// <param name="debug_method">The type of operation being performed.</param>
        /// <param name="quote">Quote to check.</param>
        /// <returns></returns>
        private bool Exists(DebugMethod debug_method, Quote quote)
        {
            if(quotes.Exists(x => x.quote == quote.quote))
            {
                BotDebug.Error(debug_method, DebugObject.Quote, DebugError.ExistYes);
                BotDebug.PrintLine(nameof(quote.quote), quote.quote);

                return true;
            }

            return false;
        }

        #endregion

        #region String parsing

        private int GetIndex(Message message)
        {
            int index = -1;

            string quote_index_string_before = message.body.TextBefore(" "),
                   quote_index_string = quote_index_string_before.CheckString() ? quote_index_string_before : message.body;

            if (!quote_index_string.CheckString())
            {
                return index;
            }

            if (!quote_index_string.CanCovertTo<int>())
            {
                return index;
            }

            index = Convert.ToInt32(quote_index_string);

            if (index > quotes.Count - 1 || index < 0)
            {
                index = -1;
            }

            return index;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="method">The type of operation being performed.</param>
        /// <param name="commands">Used to parse the <see cref="Message.body"/> for the quote.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="broadcaster">Contains the name of the broadcaster to be appended to the end of the quote.</param>
        /// <returns></returns>
        private Quote MessageToQuote(DebugMethod method, Message message, TwitchClientOAuth broadcaster)
        {
            string quote_string = message.body;

            try
            {
                Quote quote = new Quote
                {
                    quote = quote_string,
                    quotee = broadcaster.display_name,
                    date = DateTime.Now                    
                };

                BotDebug.Success(DebugMethod.Serialize, DebugObject.Quote, quote.quote);
                BotDebug.PrintObject(quote);

                return quote;
            }
            catch (Exception exception)
            {
                Notify.Error(method, DebugObject.Quote, quote_string, DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Serialize, DebugObject.Quote, DebugError.Exception);
                BotDebug.Error(method, DebugObject.Quote, DebugError.Null);
                BotDebug.PrintLine(nameof(quote_string), quote_string);
                BotDebug.PrintLine(nameof(exception), exception.Message);

                return default(Quote);
            }
        }

        #endregion
    }
}
