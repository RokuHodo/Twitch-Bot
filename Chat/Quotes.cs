using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

using TwitchBot.Clients;
using TwitchBot.Debugger;
using TwitchBot.Enums.Debugger;
using TwitchBot.Enums.Extensions;
using TwitchBot.Extensions;
using TwitchBot.Extensions.Files;
using TwitchBot.Models.Bot.Chat;
using TwitchBot.Parser;

namespace TwitchBot.Chat
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

            DebugBot.BlankLine();

            DebugBot.Notify("Loading Quotes");
            DebugBot.PrintLine("File path:", FILE_PATH);

            quotes_preloaded = File.ReadAllText(FILE_PATH);
            quotes_preloaded_list = JsonConvert.DeserializeObject<List<Quote>>(quotes_preloaded);

            if (quotes_preloaded_list != null)
            {
                foreach (Quote quote in quotes_preloaded_list)
                {
                    Load(quote);
                }
            }
        }

        #region Load quotes

        /// <summary>
        /// Loads a <see cref="Quote"/> into the <see cref="quotes"/> list to be called in real time.
        /// </summary>
        /// <param name="command">The command to load.</param>
        private void Load(Quote quote)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Loading quote...");

            if (quote == default(Quote))
            {
                DebugBot.Error(DebugMethod.LOAD, nameof(quote), DebugError.NORMAL_EXCEPTION);

                return;
            }

            if (Exists(quote))
            {
                DebugBot.Error(DebugMethod.LOAD, nameof(quote), DebugError.NORMAL_EXISTS_YES);
                DebugBot.PrintLine(nameof(quote.quote), quote.quote);

                return;
            }

            try
            {
                quotes.Add(quote);

                DebugBot.Success(DebugMethod.LOAD, nameof(quote));
                DebugBot.PrintObject(quote);
            }
            catch (Exception exception)
            {
                DebugBot.Error(DebugMethod.LOAD, nameof(quote), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(quote.quote), quote.quote);
                DebugBot.PrintLine(nameof(exception), exception.Message);

                return;
            }
        }

        #endregion

        #region Add quotes

        /// <summary>
        /// Modify the variables by adding, editting, or removing.
        /// </summary>
        /// <param name="commands">Used for parsing the body.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="TwitchNotify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        public void Modify(Commands commands, MessageTwitch message, TwitchClientOAuth broadcaster, TwitchClientOAuth bot)
        {
            string temp = commands.ParseAfterCommand(message),
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
                DebugBot.Error(DebugMethod.MODIFY, "quote", DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
                DebugBot.PrintLine(nameof(temp), temp);
                DebugBot.PrintLine(nameof(key), key);
                DebugBot.PrintLine(nameof(message.body), message.body);
            }
        }

        /// <summary>
        /// Parses the <see cref="MessageTwitch.body"/> of a message and adds a <see cref="Quote"/> and adds it to the <see cref="quotes"/> list to be called in real time.
        /// Called by the user from Twitch chat by using the "!addquote" command.
        /// </summary>
        /// <param name="commands">Used to parse the message for the quote.</param>        
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="TwitchNotify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="broadcaster">Coontains the broadcaster name to be appended to the end of the quote</param>
        public void Add(MessageTwitch message, TwitchClientOAuth broadcaster)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Adding quote...");

            Quote quote = MessageToQuote(message, broadcaster);

            if (quote == default(Quote))
            {
                TwitchNotify.Error(DebugMethod.ADD, message, nameof(quote), message.body, DebugError.NORMAL_SERIALIZE);

                DebugBot.Error(DebugMethod.ADD, nameof(quote), DebugError.NORMAL_SERIALIZE);
                DebugBot.PrintLine(nameof(message.body), message.body);

                return;
            }

            if (Exists(quote))
            {
                TwitchNotify.Error(DebugMethod.ADD, message, nameof(quote), quote.quote, DebugError.NORMAL_EXISTS_YES);

                DebugBot.Error(DebugMethod.ADD, nameof(quote), DebugError.NORMAL_EXISTS_YES);
                DebugBot.PrintLine(nameof(quote.quote), quote.quote);

                return;
            }

            try
            {
                quotes.Add(quote);

                JsonConvert.SerializeObject(quotes, Formatting.Indented).OverrideFile(FILE_PATH);

                TwitchNotify.Success(DebugMethod.ADD, message, nameof(quote), quote.quote);

                DebugBot.Success(DebugMethod.ADD, nameof(quote));
                DebugBot.PrintObject(quote);
            }
            catch (Exception exception)
            {
                TwitchNotify.Error(DebugMethod.ADD, message, nameof(quote), quote.quote, DebugError.NORMAL_EXCEPTION);

                DebugBot.Error(DebugMethod.ADD, nameof(quote), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(quote.quote), quote.quote);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        public void Edit(MessageTwitch message, TwitchClientOAuth broadcaster)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Editing quote...");                        

            if (!message.body.CheckString())
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, "quote", message.body, DebugError.NORMAL_NULL);

                DebugBot.Error(DebugMethod.EDIT, "quote", DebugError.NORMAL_NULL);
                DebugBot.PrintLine(nameof(message.body), "null");

                return;
            }

            int index = GetIndex(message);

            if(index == -1)
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, "quote", index.ToString(), DebugError.NORMAL_OUT_OF_BOUNDS);

                DebugBot.Error(DebugMethod.EDIT, "quote", DebugError.NORMAL_OUT_OF_BOUNDS);
                DebugBot.PrintLine(nameof(index), index.ToString());
                DebugBot.PrintLine(nameof(quotes.Count), quotes.Count.ToString());

                return;
            }

            message.body = message.body.TextAfter(" ").RemoveWhiteSpace(WhiteSpace.Left);

            Quote quote = MessageToQuote(message, broadcaster);
            
            if(quote == default(Quote))
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, nameof(quote), message.body, DebugError.NORMAL_SERIALIZE);

                DebugBot.Error(DebugMethod.EDIT, nameof(quote), DebugError.NORMAL_SERIALIZE);
                DebugBot.PrintLine(nameof(message.body), message.body);

                return;
            }

            try
            {
                quotes[index] = quote;

                JsonConvert.SerializeObject(quotes, Formatting.Indented).OverrideFile(FILE_PATH);

                TwitchNotify.Success(DebugMethod.EDIT, message, nameof(quote), index.ToString());

                DebugBot.Success(DebugMethod.EDIT, nameof(quote));
                DebugBot.PrintObject(quote);
                DebugBot.PrintLine(nameof(index), index.ToString());
                
            }
            catch (Exception exception)
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, nameof(quote), quote.quote, DebugError.NORMAL_EXCEPTION);

                DebugBot.Error(DebugMethod.EDIT, nameof(quote), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(quote.quote), quote.quote);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        private void Remove(MessageTwitch message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Removing quote...");

            if (!message.body.CheckString())
            {
                TwitchNotify.Error(DebugMethod.REMOVE, message, "quote", message.body, DebugError.NORMAL_NULL);

                DebugBot.Error(DebugMethod.REMOVE, "quote", DebugError.NORMAL_NULL);
                DebugBot.PrintLine(nameof(message.body), "null");

                return;
            }

            int index = GetIndex(message);

            if (index == -1)
            {
                TwitchNotify.Error(DebugMethod.REMOVE, message, "quote", index.ToString(), DebugError.NORMAL_OUT_OF_BOUNDS);

                DebugBot.Error(DebugMethod.REMOVE, "quote", DebugError.NORMAL_OUT_OF_BOUNDS);
                DebugBot.PrintLine(nameof(index), index.ToString());
                DebugBot.PrintLine(nameof(quotes.Count), quotes.Count.ToString());

                return;
            }

            try
            {
                quotes.RemoveAt(index);

                JsonConvert.SerializeObject(quotes, Formatting.Indented).OverrideFile(FILE_PATH);

                TwitchNotify.Success(DebugMethod.REMOVE, message, "quote", index.ToString());

                DebugBot.Success(DebugMethod.REMOVE, "quote");
                DebugBot.PrintLine(nameof(index), index.ToString());
            }
            catch (Exception exception)
            {
                TwitchNotify.Error(DebugMethod.REMOVE, message, "quote", message.body, DebugError.NORMAL_EXCEPTION);

                DebugBot.Error(DebugMethod.REMOVE, "quote", DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        #endregion

        #region Get quote information

        /// <summary>
        /// Gets a random <see cref="Quote"/> from the <see cref="quotes"/> list.
        /// </summary>
        /// <returns></returns>
        public string GetQuote(MessageTwitch message)
        {
            int index;

            string _quote = string.Empty;

            if(quotes.Count < 1)
            {
                TwitchNotify.SendMessage(message, "There are no quotes yet!");

                DebugBot.Error(DebugMethod.GET, "quote", DebugError.NORMAL_NULL);
                DebugBot.PrintLine(nameof(quotes.Count), quotes.Count.ToString());

                return _quote;
            }

            if (message.body.CheckString())
            {
                index = GetIndex(message);

                if (index == -1)
                {
                    TwitchNotify.Error(DebugMethod.GET, message, "quote", index.ToString(), DebugError.NORMAL_OUT_OF_BOUNDS);

                    DebugBot.Error(DebugMethod.GET, "quote", DebugError.NORMAL_OUT_OF_BOUNDS);
                    DebugBot.PrintLine(nameof(index), index.ToString());
                    DebugBot.PrintLine(nameof(quotes.Count), quotes.Count.ToString());

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
        /// Checks to see if a quote already exists.
        /// </summary>
        /// /// <param name="debug_method">The type of operation being performed.</param>
        /// <param name="quote">Quote to check.</param>
        /// <returns></returns>
        private bool Exists(Quote quote)
        {
            if(quotes.Exists(x => x.quote == quote.quote))
            {               
                return true;
            }

            return false;
        }

        #endregion

        #region String parsing

        private int GetIndex(MessageTwitch message)
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
        /// <param name="commands">Used to parse the <see cref="MessageTwitch.body"/> for the quote.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="TwitchNotify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="broadcaster">Contains the name of the broadcaster to be appended to the end of the quote.</param>
        /// <returns></returns>
        private Quote MessageToQuote(MessageTwitch message, TwitchClientOAuth broadcaster)
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

                DebugBot.Success(DebugMethod.SERIALIZE, nameof(quote));
                DebugBot.PrintObject(quote);

                return quote;
            }
            catch (Exception exception)
            {
                DebugBot.Error(DebugMethod.SERIALIZE, "quote", DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(quote_string), quote_string);
                DebugBot.PrintLine(nameof(exception), exception.Message);

                return default(Quote);
            }
        }

        #endregion
    }
}
