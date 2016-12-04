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
using TwitchBot.Messages;
using TwitchBot.Models.Bot.Chat;

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
        /// Loads all the <see cref="Quote"/>s from the <see cref="FILE_PATH"/>.
        /// </summary>
        private void Load(Quote quote)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Loading quote...");

            //the quote is "empty", doon't load it
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
        /// Modify commands by adding, editting, or removing commands.
        /// </summary>        
        public void Modify(Commands commands, TwitchMessage message, TwitchClientOAuth broadcaster, TwitchClientOAuth bot)
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
        /// Adds a <see cref="Quote"/> at run time without needing to re-launch the bot.
        /// </summary>
        public void Add(TwitchMessage message, TwitchClientOAuth broadcaster)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Adding quote...");

            Quote quote = MessageToQuote(message, broadcaster);

            //the chat message could not be serialized into a quote
            if (quote == default(Quote))
            {
                TwitchNotify.Error(DebugMethod.ADD, message, nameof(quote), message.body, DebugError.NORMAL_DESERIALIZE);

                DebugBot.Error(DebugMethod.ADD, nameof(quote), DebugError.NORMAL_DESERIALIZE);
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

        /// <summary>
        /// Edits a pre-existing <see cref="Quote"/> based on its index at run time without needing to re-launch the bot.
        /// </summary>
        public void Edit(TwitchMessage message, TwitchClientOAuth broadcaster)
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

            //index specified by the user was out of range
            if (index < 0 || index > quotes.Count - 1)
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, "quote", index.ToString(), DebugError.NORMAL_OUT_OF_BOUNDS);

                DebugBot.Error(DebugMethod.EDIT, "quote", DebugError.NORMAL_OUT_OF_BOUNDS);
                DebugBot.PrintLine(nameof(index), index.ToString());
                DebugBot.PrintLine(nameof(quotes.Count), quotes.Count.ToString());

                return;
            }

            //the quote body will be anything after the index
            message.body = message.body.TextAfter(" ").RemovePadding(Padding.Left);

            Quote quote = MessageToQuote(message, broadcaster);

            //the chat message could not be deserialized into a quote
            if (quote == default(Quote))
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, nameof(quote), message.body, DebugError.NORMAL_DESERIALIZE);

                DebugBot.Error(DebugMethod.EDIT, nameof(quote), DebugError.NORMAL_DESERIALIZE);
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

        /// <summary>
        /// Removes a pre-existing <see cref="Quote"/> based on its index at run time without needing to re-launch the bot.
        /// </summary>
        private void Remove(TwitchMessage message)
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

            //index specified by the user was out of range
            if (index < 0 || index > quotes.Count - 1)
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
        /// Gets a <see cref="Quote"/> to be printed to twitch.
        /// A user can either specify a quote by using !quote (index) or get a random quote by just using !quote.
        /// </summary>
        public string GetQuote(TwitchMessage message)
        {
            int index;

            string quote = string.Empty;

            if(quotes.Count < 1)
            {
                TwitchNotify.SendMessage(message, "There are no quotes yet!");

                DebugBot.Error(DebugMethod.GET, "quote", DebugError.NORMAL_NULL);
                DebugBot.PrintLine(nameof(quotes.Count), quotes.Count.ToString());

                return quote;
            }

            //there is something after !quote and it's not a modifier, assume the user is trying to specify an index
            if (message.body.CheckString())
            {
                index = GetIndex(message);

                if (index < 0 || index > quotes.Count - 1)
                {
                    TwitchNotify.Error(DebugMethod.GET, message, nameof(quote), index.ToString(), DebugError.NORMAL_OUT_OF_BOUNDS);

                    DebugBot.Error(DebugMethod.GET, "quote", DebugError.NORMAL_OUT_OF_BOUNDS);
                    DebugBot.PrintLine(nameof(index), index.ToString());
                    DebugBot.PrintLine(nameof(quotes.Count), quotes.Count.ToString());

                    return quote;
                }
            }
            else
            {
                index = new Random().Next(quotes.Count);
            }

            quote = quotes[index].quote + " - " + quotes[index].quotee + " " + quotes[index].date;

            return quote;
        }

        /// <summary>
        /// Counts how many quotes are loaded and returns a message to be printed to twitch.
        /// </summary>        
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
        /// Checks to see if a <see cref="Quote"/> already exists.
        /// </summary>
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

        /// <summary>
        /// Gets the text after !quote and attempts to convert it into an integer.
        /// </summary>
        private int GetIndex(TwitchMessage message)
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

            return index;
        }

        /// <summary>
        /// Converts a <see cref="TwitchMessage"/> recieved from Twitch and attempts to deserialize the body in to a <see cref="Quote"/>.
        /// </summary>
        private Quote MessageToQuote(TwitchMessage message, TwitchClientOAuth broadcaster)
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
