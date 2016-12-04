using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using TwitchBot.Clients;
using TwitchBot.Debugger;
using TwitchBot.Enums.Chat;
using TwitchBot.Enums.Debugger;
using TwitchBot.Enums.Extensions;
using TwitchBot.Extensions;
using TwitchBot.Extensions.Files;
using TwitchBot.Messages;
using TwitchBot.Models.Bot.Spam;

namespace TwitchBot.Chat
{
    class SpamFilter
    {
        readonly string FILE_PATH_BANNED_USERS = Environment.CurrentDirectory + "/JSON/Spam Filter/Banned USers.json",
                        FILE_PATH_BLACKLISTED_WORDS = Environment.CurrentDirectory + "/JSON/Spam Filter/Blacklisted Words.json",
                        FILE_PATH_SETTINGS = Environment.CurrentDirectory + "/JSON/Spam Filter/Spam Settings.json";

        List<string> banned_users_list,
                     blacklisted_words_list;

        Dictionary<string, int> timeout_tracker;

        SpamSettings spam_settings_master;        

        public SpamFilter()
        {
            banned_users_list = new List<string>();
            blacklisted_words_list = new List<string>();
            timeout_tracker = new Dictionary<string, int>();


            DebugBot.BlankLine();
            DebugBot.Notify("Loading Banned Users");
            DebugBot.PrintLine("File path:", FILE_PATH_BANNED_USERS);

            Load_BannedUsers(FILE_PATH_BANNED_USERS);

            DebugBot.BlankLine();
            DebugBot.Notify("Loading Blacklisted Words");
            DebugBot.PrintLine("File path:", FILE_PATH_BLACKLISTED_WORDS);
            DebugBot.BlankLine();

            Load_BlacklistedWords(FILE_PATH_BLACKLISTED_WORDS);

            DebugBot.BlankLine();
            DebugBot.Notify("Loading Spam Filter Settings");
            DebugBot.PrintLine("File path:", FILE_PATH_SETTINGS);
            DebugBot.BlankLine();

            Load_Settings(FILE_PATH_SETTINGS);                                         
        }

        #region Load spam settings

        /// <summary>
        /// Loads all users that were banned by the bot.
        /// </summary>
        private void Load_BannedUsers(string file_path)
        {           
            try
            {               
                string banned_users_preloaded = File.ReadAllText(file_path);

                if (!banned_users_preloaded.CheckString())
                {
                    DebugBot.BlankLine();
                    DebugBot.Warning("No banned users found");

                    return;
                }

                List<string> users = JsonConvert.DeserializeObject<List<string>>(banned_users_preloaded);

                if (users.Count == users.Distinct().Count())
                {
                    banned_users_list = users;
                }
                else
                {
                    banned_users_list = users.Distinct().ToList();
                }
            }
            catch(Exception exception)
            {
                DebugBot.Error(DebugMethod.LOAD, nameof(banned_users_list), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }            
        }

        /// <summary>
        /// Loads all blacklisted words and phrases to be filtered by the bot.
        /// </summary>        
        private void Load_BlacklistedWords(string file_path)
        {
            try
            {
                string blacklisted_words_preloaded = File.ReadAllText(file_path);

                if (!blacklisted_words_preloaded.CheckString())
                {
                    DebugBot.BlankLine();
                    DebugBot.Warning("No blacklisted words found");

                    return;
                }

                List<string> words = JsonConvert.DeserializeObject<List<string>>(blacklisted_words_preloaded);

                if (words.Count == words.Distinct().Count())
                {
                    blacklisted_words_list = words;
                }
                else
                {
                    blacklisted_words_list = words.Distinct().ToList();
                }

                DebugBot.SubHeader("Blacklisted words");
                DebugBot.PrintObject(blacklisted_words_list);
            }
            catch(Exception exception)
            {
                DebugBot.Error(DebugMethod.LOAD, nameof(blacklisted_words_list), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Loads all spam settings that determine when twitch messages get filtered for spam.
        /// </summary>
        private void Load_Settings(string file_path)
        {
            try
            {
                string settings = File.ReadAllText(file_path);

                if (!settings.CheckString())
                {
                    DebugBot.BlankLine();
                    DebugBot.Warning("No spam settings found");

                    return;
                }

                spam_settings_master = JsonConvert.DeserializeObject<SpamSettings>(settings);

                DebugBot.SubHeader("Spam settings");
                DebugBot.PrintObject(spam_settings_master);
            }
            catch(Exception exception)
            {
                DebugBot.Error(DebugMethod.LOAD, nameof(spam_settings_master), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }          
        }

        #endregion

        #region Spam checks

        /// <summary>
        /// Checks if the body of a <see cref="TwitchMessage"/> contains any spam by passing it through each filter.
        /// </summary>
        public bool ContainsSpam(TwitchMessage message, TwitchClientOAuth bot, TwitchClientOAuth broadcaster)
        {
            bool result = false;

            //make sure there are any filters to reun the messagr through
            if(spam_settings_master == null || spam_settings_master == default(SpamSettings))
            {
                return result;
            }

            //keep track of how mnay times the person has been timed out by the bot
            if (!timeout_tracker.ContainsKey(message.sender.name))
            {
                timeout_tracker[message.sender.name] = 0;
            }

            //check to see if the user has the minimum global user-type to be un-affected by the filter
            if (message.sender.MeetskPermissionRequirement(message.sender.user_type, spam_settings_master.permission) || !spam_settings_master.enabled)
            {
                return result;
            }

            //check for ascii spam, supports english only at the moment
            if (ContainsSpam_ASCII(message, spam_settings_master.ASCII))
            {
                Timeout(bot, broadcaster, message.sender.name, "excessive ascii", spam_settings_master.timeouts);

                result = true;
            }

            //check if the message contains any blacklisted words or phrases
            if (ContainsSpam_Blacklist(message, spam_settings_master.Blacklist, blacklisted_words_list))
            {
                Timeout(bot, broadcaster, message.sender.name, "use of blacklisted word(s)", spam_settings_master.timeouts);

                result = true;
            }

            //check to see if they used caps a little too much
            if (ContainsSpam_Caps(message, spam_settings_master.Caps))
            {
                Timeout(bot, broadcaster, message.sender.name, "excessive caps", spam_settings_master.timeouts);

                result = true;
            }

            //check to see if the message contains any links
            if (ContainsSpam_Links(message, spam_settings_master.Links))
            {
                Timeout(bot, broadcaster, message.sender.name, "posting links", spam_settings_master.timeouts);

                result = true;
            }

            //check the overall length of the message to avoid walls of text
            if (ContainsSpam_Wall(message, spam_settings_master.Wall))
            {
                Timeout(bot, broadcaster, message.sender.name, "wall of text", spam_settings_master.timeouts);

                result = true;
            }                               

            return result;
        }        

        /// <summary>
        /// Checks the body of a <see cref="TwitchMessage"/> for excessive use of ASCII symbols.
        /// The range of ASCII symbols to search for is hard coded at the moment.
        /// </summary>
        private bool ContainsSpam_ASCII(TwitchMessage message, ASCII settings)
        {
            bool result = false;

            //the player has the minimum required user-typer to be un-affected by the filter
            if (message.sender.MeetskPermissionRequirement(message.sender.user_type, settings.permission) || !settings.enabled)
            {
                return result;
            }

            //the body length was less than the required length to check for spam
            if (message.body.Length < settings.length)
            {
                return result;
            }

            int characters_ascii = 0;

            //check the message again without white space
            string body_no_whitespace = message.body.RemoveWhiteSpace();

            if (body_no_whitespace.Length < settings.length)
            {
                return result;
            }

            byte[] ascii_bytes = Encoding.GetEncoding(437).GetBytes(body_no_whitespace.ToCharArray());

            foreach (byte _byte in ascii_bytes)
            {
                if (_byte > 175 && _byte < 224 || _byte == 254)
                {
                    ++characters_ascii;
                }
            }

            //returns false if the calculated percent is lower than the max allowable percent
            result = characters_ascii.ExceedsMaxAllowablePercent(ascii_bytes.Length, settings.percent);

            return result;
        }

        /// <summary>
        /// Checks the body of a <see cref="TwitchMessage"/> for use of any blakcklisted words or phrases.
        /// </summary>
        private bool ContainsSpam_Blacklist(TwitchMessage message, Blacklist settings, List<string> blacklist)
        {
            bool result = false;

            if(blacklisted_words_list.Count == 0)
            {
                return result;
            }

            string error = string.Empty;

            string[] words = message.body.ToLower().StringToArray<string>(' ');

            //the player has the minimum required user-typer to be un-affected by the filter
            if (message.sender.MeetskPermissionRequirement(message.sender.user_type, settings.permission) || !settings.enabled)
            {
                return result;
            }

            Match match;

            foreach (string blacklisted_word in blacklist)
            {
                //look for any use of the word/phrase
                if (blacklisted_word.StartsWith("*"))
                {
                    string _blacklisted_word = blacklisted_word.Substring(1).ToLower();

                    if (message.body.Contains(_blacklisted_word))
                    {
                        result = true;
                    }
                }
                //look for exact uses of the word/phrase
                else
                {
                    match = Regex.Match(message.body, @"\b" + blacklisted_word + @"\b", RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        return true;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Checks the body of a <see cref="TwitchMessage"/> for excessive use of caps.
        /// </summary>
        private bool ContainsSpam_Caps(TwitchMessage message, Caps settings)
        {
            bool result = false;

            //the player has the minimum required user-typer to be un-affected by the filter
            if (message.sender.MeetskPermissionRequirement(message.sender.user_type, settings.permission) || !settings.enabled)
            {
                return result;
            }

            //the body length was less than the required length to check for spam
            if (message.body.Length < settings.length)
            {
                return result;
            }
            
            int characters_uppercase = 0;

            string body_no_whitespace = message.body.RemoveWhiteSpace();

            byte[] ascii_bytes = Encoding.ASCII.GetBytes(body_no_whitespace.ToCharArray());

            //this method only supports english for now
            foreach(byte _byte in ascii_bytes)
            {
                if(_byte > 64 && _byte < 91)
                {
                    ++characters_uppercase;
                }
            }

            //returns false if the calculated percent is lower than the max allowable percent
            result = characters_uppercase.ExceedsMaxAllowablePercent(body_no_whitespace.Length, settings.percent);

            return result;
        }
        /// <summary>
        /// Checks the body of a <see cref="TwitchMessage"/> for any links.
        /// </summary>
        private bool ContainsSpam_Links(TwitchMessage message, Links settings)
        {
            bool result = false;

            //the player has the minimum required user-typer to be un-affected by the filter
            if (message.sender.MeetskPermissionRequirement(message.sender.user_type, settings.permission) || !settings.enabled)
            {
                return result;
            }

            MatchCollection matches = Regex.Matches(message.body, @"([a-zA-Z0-9]+)\.([a-zA-z]{2,})", RegexOptions.IgnoreCase);

            if(matches.Count > 0)
            {
                result = true;                
            }

            return result;
        }

        /// <summary>
        /// Checks the body of a <see cref="TwitchMessage"/> for its length and see if it's greater than a user defined threshold.
        /// </summary>
        private bool ContainsSpam_Wall(TwitchMessage message, Wall settings)
        {
            bool result = false;

            //the player has the minimum required user-typer to be un-affected by the filter
            if (message.sender.MeetskPermissionRequirement(message.sender.user_type, settings.permission) || !settings.enabled)
            {
                return result;
            }

            //the body length was less than the required length to check for spam
            if (message.body.Length < settings.length)
            {
                return result;
            }

            string body = message.body.RemovePadding();

            if(body.Length > settings.length)
            {
                result = true;
            }

            return result;
        }

        #endregion

        #region Spam timeouts

        /// <summary>
        /// Times out a user with a specified reason.
        /// The timeout length is determined by how many times the user has been timed out by the bot and the timeout spam settings.
        /// </summary>
        private void Timeout(TwitchClientOAuth bot, TwitchClientOAuth broadcaster, string sender, string reason, int[] timeout_increments)
        {
            if (timeout_tracker[sender] < timeout_increments.Length)
            {
                bot.SendWhisper(sender, "Timed out for " + reason + ". [warning]");
                broadcaster.Timeout(sender, timeout_increments[timeout_tracker[sender]], reason + " [warning - bot]");

                DebugBot.PrintLine("\"" + sender + "\" has been timed out for \"" + timeout_increments[timeout_tracker[sender]] + "\" second(s)");
                DebugBot.PrintLine(nameof(reason), reason);
            }
            else
            {
                if (!banned_users_list.Contains(sender))
                {
                    banned_users_list.Add(sender);

                    JsonConvert.SerializeObject(banned_users_list, Formatting.Indented).OverrideFile(FILE_PATH_BANNED_USERS);
                }

                bot.SendWhisper(sender, "Banned for " + reason + ".");
                broadcaster.Ban(sender, reason + " [bot]");

                DebugBot.PrintLine("\"" + sender + "\" has been banned.");
                DebugBot.PrintLine(nameof(reason), reason);
            }

            ++timeout_tracker[sender];
        }

        #endregion

        #region Change and apply spam settings

        /// <summary>
        /// Modify the blacklisted words by adding, editing, or removing them.
        /// </summary>
        public void Modify_BlacklistedWords(Commands commands, TwitchMessage message)
        {
            string temp = commands.ParseAfterCommand(message),
                   key = temp.TextBefore(" ");

            message.body = temp.TextAfter(" ");

            try
            {
                switch (key)
                {
                    case "!add":
                        Add_BlacklistedWord(message);
                        break;
                    case "!edit":
                        Edit_BlacklistedWord(message);
                        break;
                    case "!remove":
                        Remove_BlacklistedWord(message);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception exception)
            {
                DebugBot.Error(DebugMethod.MODIFY, nameof(blacklisted_words_list), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
                DebugBot.PrintLine(nameof(temp), temp);
                DebugBot.PrintLine(nameof(key), key);
                DebugBot.PrintLine(nameof(message.body), message.body);
            }
        }

        /// <summary>
        /// Adds blacklisted words/phrases at run time to be checked in real time without needing to re-launch the bot.
        /// </summary>
        private void Add_BlacklistedWord(TwitchMessage message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Adding blacklisted words...");

            bool list_modified = false;

            string blacklisted_words = message.body;

            string[] blacklisted_words_array;

            if (!blacklisted_words.CheckString())
            {
                DebugBot.Error(DebugMethod.ADD, nameof(blacklisted_words_list), DebugError.NORMAL_NULL);
                DebugBot.PrintLine(nameof(blacklisted_words), "null");

                return;
            }

            blacklisted_words_array = blacklisted_words.StringToArray<string>(',');

            try
            {
                foreach (string word in blacklisted_words_array)
                {
                    string _word = word.RemovePadding();

                    if (blacklisted_words_list.Contains(_word))
                    {
                        DebugBot.Error(DebugMethod.ADD, nameof(blacklisted_words_list), DebugError.NORMAL_EXISTS_YES);
                    }
                    else
                    {
                        DebugBot.Success(DebugMethod.ADD, nameof(blacklisted_words_list));

                        blacklisted_words_list.Add(_word);

                        list_modified = true;
                    }

                    DebugBot.PrintLine(nameof(word), _word);
                }

                if (list_modified)
                {
                    JsonConvert.SerializeObject(blacklisted_words_list, Formatting.Indented).OverrideFile(FILE_PATH_BLACKLISTED_WORDS);

                    TwitchNotify.Success(DebugMethod.ADD, message, "blacklisted word(s)");                  
                }
                else
                {
                    TwitchNotify.Error(DebugMethod.ADD, message, "blacklisted word(s)", "no new blacklisted words found");
                }
            }
            catch (Exception exception)
            {
                TwitchNotify.Error(DebugMethod.ADD, message, "blacklisted word(s)", string.Empty, DebugError.NORMAL_EXCEPTION);

                DebugBot.Error(DebugMethod.ADD, nameof(blacklisted_words_list), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Edits pre-existing blacklisted words/phrases at run time to be checked in real time without needing to re-launch the bot.
        /// Currently only one word/phrase can be edited at a time.
        /// </summary>
        private void Edit_BlacklistedWord(TwitchMessage message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Editting blacklisted word...");

            string blacklisted_words = message.body;

            string[] blacklisted_words_array;

            //there was no text after the modifier, do nothing
            if (!blacklisted_words.CheckString())
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, "blacklisted word(s)", string.Empty, DebugError.NORMAL_NULL);

                DebugBot.Error(DebugMethod.EDIT, nameof(blacklisted_words_list), DebugError.NORMAL_NULL);
                DebugBot.PrintLine(nameof(blacklisted_words), "null");

                return;
            }

            blacklisted_words_array = blacklisted_words.StringToArray<string>(',');

            //only allow one word to be editted at a time now 
            if (blacklisted_words_array.Length != 2)
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, "blacklisted word(s)", blacklisted_words_array[0], DebugError.SYNTAX_LENGTH);

                DebugBot.Error(DebugMethod.EDIT, nameof(blacklisted_words_array), DebugError.SYNTAX_LENGTH);
                DebugBot.PrintLine(nameof(blacklisted_words_array.Length), blacklisted_words_array.Length.ToString());
                DebugBot.PrintLine("required length", "2");

                DebugBot.Error(DebugMethod.EDIT, nameof(blacklisted_words_list), DebugError.NORMAL_SYNTAX);

                return;
            }

            //word/phrase doesn't exist
            if (!blacklisted_words_list.Contains(blacklisted_words_array[0]))
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, "blacklisted word(s)", blacklisted_words_array[0], DebugError.NORMAL_EXISTS_NO);

                DebugBot.Error(DebugMethod.EDIT, nameof(blacklisted_words_list), DebugError.NORMAL_EXISTS_NO);
                DebugBot.PrintLine("word", blacklisted_words_array[0]);

                return;
            }

            try
            {
                blacklisted_words_list.Remove(blacklisted_words_array[0]);
                blacklisted_words_list.Add(blacklisted_words_array[1].RemovePadding());

                JsonConvert.SerializeObject(blacklisted_words_list, Formatting.Indented).OverrideFile(FILE_PATH_BLACKLISTED_WORDS);

                TwitchNotify.Success(DebugMethod.EDIT, message, "blacklisted word(s)", blacklisted_words_array[0] + " -> " + blacklisted_words_array[1]);

                DebugBot.Success(DebugMethod.EDIT, nameof(blacklisted_words_list));
                DebugBot.PrintLine("old word", blacklisted_words_array[0]);
                DebugBot.PrintLine("new word", blacklisted_words_array[1].RemovePadding());
            }
            catch (Exception exception)
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, "blacklisted word(s)", blacklisted_words_array[0], DebugError.NORMAL_EXCEPTION);

                DebugBot.Error(DebugMethod.EDIT, nameof(blacklisted_words_list), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Removes pre-existing blacklisted words/phrases at run time without needing to re-launch the bot.
        /// </summary>
        /// <param name="message"></param>
        private void Remove_BlacklistedWord(TwitchMessage message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Removing blacklisted words...");

            bool list_modified = false;

            string blacklisted_words = message.body;

            string[] blacklisted_words_array;

            if (!blacklisted_words.CheckString())
            {
                DebugBot.Error(DebugMethod.REMOVE, nameof(blacklisted_words_list), DebugError.NORMAL_NULL);
                DebugBot.PrintLine(nameof(blacklisted_words), "null");

                return;
            }

            blacklisted_words_array = blacklisted_words.StringToArray<string>(',');

            try
            {
                foreach (string word in blacklisted_words_array)
                {
                    string _word = word.RemovePadding();

                    if (!blacklisted_words_list.Contains(_word))
                    {
                        DebugBot.Error(DebugMethod.REMOVE, nameof(blacklisted_words_list), DebugError.NORMAL_EXISTS_NO);
                    }
                    else
                    {                       
                        blacklisted_words_list.Remove(_word);

                        DebugBot.Success(DebugMethod.REMOVE, nameof(blacklisted_words_list));
                        DebugBot.PrintLine(nameof(word), _word);

                        list_modified = true;
                    }

                    DebugBot.PrintLine(nameof(word), _word);
                }

                if (list_modified)
                {
                    TwitchNotify.Success(DebugMethod.REMOVE, message, "blacklisted word(s)");

                    JsonConvert.SerializeObject(blacklisted_words_list, Formatting.Indented).OverrideFile(FILE_PATH_BLACKLISTED_WORDS);
                }
            }
            catch (Exception exception)
            {
                TwitchNotify.Error(DebugMethod.REMOVE, message, "blacklisted word(s)", string.Empty, DebugError.NORMAL_EXCEPTION);

                DebugBot.Error(DebugMethod.REMOVE, nameof(blacklisted_words_list), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }             

        /// <summary>
        /// Changes a <see cref="SpamSetting"/> through twitch chat at run time without needeing to re-launch the bot.
        /// Only the fields that have specified/changed will actually be updated.
        /// </summary>
        public void ChangeSetting(TwitchMessage message, Commands commands)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Updating spam setting...");

            bool updated = false;

            string preserialized,
                   body = commands.ParseAfterCommand(message);

            //the text after the modifier couldn't be converted into a setting, do nothing
            SpamSetting spam_setting = GetSetting(body);

            if(spam_setting == SpamSetting.None)
            {
                TwitchNotify.Error(DebugMethod.UPDATE, message, "spam setting", spam_setting.ToString(), DebugError.NORMAL_NULL);

                DebugBot.Error(DebugMethod.UPDATE, nameof(spam_settings_master), DebugError.NORMAL_NULL);
                DebugBot.PrintLine(nameof(spam_setting), "null");

                return;
            }

            SpamSettings setting = MessageToSetting(body, commands, spam_setting, out preserialized);

            if(setting == null)
            {
                TwitchNotify.Error(DebugMethod.UPDATE, message, "spam setting", string.Empty, DebugError.NORMAL_NULL);

                DebugBot.Error(DebugMethod.UPDATE, nameof(spam_settings_master), DebugError.NORMAL_NULL);
                DebugBot.PrintLine(nameof(setting), "null");

                return;
            }

            //TODO: have this done automatically instead of manually specifying each field
            try
            {
                switch (spam_setting)
                {
                    case SpamSetting.ASCII:
                        spam_settings_master.ASCII.enabled = ApplySetting<bool>(setting.ASCII.enabled, spam_settings_master.ASCII.enabled, "enabled", preserialized);
                        spam_settings_master.ASCII.length = ApplySetting<int>(setting.ASCII.length, spam_settings_master.ASCII.length, "length", preserialized);
                        spam_settings_master.ASCII.percent = ApplySetting<int>(setting.ASCII.percent, spam_settings_master.ASCII.percent, "percent", preserialized);
                        spam_settings_master.ASCII.permission = ApplySetting<UserType>(setting.ASCII.permission, spam_settings_master.ASCII.permission, "permission", preserialized);                                               

                        DebugBot.Success(DebugMethod.UPDATE, nameof(spam_settings_master));
                        DebugBot.PrintLine(nameof(spam_setting), nameof(spam_settings_master.ASCII));
                        DebugBot.PrintObject(spam_settings_master.ASCII);

                        updated = true;

                        break;
                    case SpamSetting.Caps:
                        spam_settings_master.Caps.enabled = ApplySetting<bool>(setting.Caps.enabled, spam_settings_master.Caps.enabled, "enabled", preserialized);
                        spam_settings_master.Caps.length = ApplySetting<int>(setting.Caps.length, spam_settings_master.Caps.length, "length", preserialized);
                        spam_settings_master.Caps.percent = ApplySetting<int>(setting.Caps.percent, spam_settings_master.Caps.percent, "percent", preserialized);
                        spam_settings_master.Caps.permission = ApplySetting<UserType>(setting.Caps.percent, spam_settings_master.Caps.permission, "permission", preserialized);

                        DebugBot.Success(DebugMethod.UPDATE, nameof(spam_settings_master));
                        DebugBot.PrintLine(nameof(spam_setting), nameof(spam_settings_master.Caps));
                        DebugBot.PrintObject(spam_settings_master.Caps);

                        updated = true;

                        break;
                    case SpamSetting.Links:
                        spam_settings_master.Links.enabled = ApplySetting<bool>(setting.Links.enabled, spam_settings_master.Links.enabled, "enabled", preserialized);
                        spam_settings_master.Links.permission = ApplySetting<UserType>(setting.Links.permission, spam_settings_master.Links.permission, "permission", preserialized);

                        DebugBot.Success(DebugMethod.UPDATE, nameof(spam_settings_master));
                        DebugBot.PrintLine(nameof(spam_setting), nameof(spam_settings_master.Links));
                        DebugBot.PrintObject(spam_settings_master.Links);

                        updated = true;

                        break;
                    case SpamSetting.Wall:
                        spam_settings_master.Wall.enabled = ApplySetting<bool>(setting.Wall.enabled, spam_settings_master.Wall.enabled, "enabled", preserialized);
                        spam_settings_master.Wall.length = ApplySetting<int>(setting.Wall.length, spam_settings_master.Wall.length, "length", preserialized);                        
                        spam_settings_master.Wall.permission = ApplySetting<UserType>(setting.Wall.permission, spam_settings_master.Wall.permission, "permission", preserialized);

                        DebugBot.Success(DebugMethod.UPDATE, nameof(spam_settings_master));
                        DebugBot.PrintLine(nameof(spam_setting), nameof(spam_settings_master.Wall));
                        DebugBot.PrintObject(spam_settings_master.Wall);

                        updated = true;

                        break;
                    case SpamSetting.enabled:
                        spam_settings_master.enabled = ApplySetting<bool>(setting.enabled, spam_settings_master.enabled, "enabled", preserialized);

                        DebugBot.Success(DebugMethod.UPDATE, nameof(spam_settings_master));
                        DebugBot.PrintLine(nameof(spam_setting), nameof(spam_settings_master.enabled));
                        DebugBot.PrintLine(nameof(spam_settings_master.enabled), spam_settings_master.enabled.ToString());

                        updated = true;

                        break;
                    case SpamSetting.permission:
                        spam_settings_master.permission = ApplySetting<UserType>(setting.permission, spam_settings_master.permission, "permission", preserialized);

                        DebugBot.Success(DebugMethod.UPDATE, nameof(spam_settings_master));
                        DebugBot.PrintLine(nameof(spam_setting), nameof(spam_settings_master.permission));
                        DebugBot.PrintLine(nameof(spam_settings_master.permission), spam_settings_master.permission.ToString());

                        updated = true;

                        break;
                    case SpamSetting.timeouts:
                        spam_settings_master.timeouts = ApplySetting<int[]>(setting.timeouts, spam_settings_master.timeouts, "timeouts", preserialized);

                        DebugBot.Success(DebugMethod.UPDATE, nameof(spam_settings_master));
                        DebugBot.PrintLine(nameof(spam_settings_master.timeouts));
                        DebugBot.PrintObject(spam_settings_master.timeouts);

                        updated = true;

                        break;
                    default:
                        break;
                }      

                if (updated)
                {
                    TwitchNotify.Success(DebugMethod.UPDATE, message, "spam setting", spam_setting.ToString());
                }               

                JsonConvert.SerializeObject(spam_settings_master, Formatting.Indented).OverrideFile(FILE_PATH_SETTINGS);
            }
            catch(Exception exception)
            {
                TwitchNotify.Error(DebugMethod.UPDATE, message, "spam settings", spam_setting.ToString(), DebugError.NORMAL_EXCEPTION);

                DebugBot.Error(DebugMethod.UPDATE, nameof(spam_settings_master), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Applies the <see cref="SpamSetting"/> field changed at run time. Only fields that have changed will be updated.
        /// </summary>
        private type ApplySetting<type>(object spam_setting_new, object spam_setting_current, string name, string setting_preserialized)
        {
            if(spam_setting_new == spam_setting_current)
            {
                DebugBot.Warning("Warning: the new setting for \"" + name + "\" is the same as the current setting. Setting not applied.");

                return (type)spam_setting_current;
            }

            if(spam_setting_new is Enum)
            {
                int size = Enum.GetNames(typeof(type)).Length - 1,
                permission_value = Convert.ToInt32(spam_setting_new);

                if (permission_value > size)
                {
                    DebugBot.Error(DebugMethod.APPLY, nameof(spam_setting_new), DebugError.SYNTAX_OUR_OF_BOUNDS);
                    DebugBot.PrintLine(nameof(spam_setting_new), spam_setting_new.ToString());
                    DebugBot.PrintLine(nameof(permission_value), permission_value.ToString());
                    DebugBot.PrintLine(nameof(size), size.ToString());

                    return (type)spam_setting_current;
                }
            }

            if(spam_setting_new is int && (int)spam_setting_new < 0)
            {
                DebugBot.Error(DebugMethod.APPLY, nameof(spam_setting_new), DebugError.SYNTAX_POSITIVE_YES);
                DebugBot.PrintLine(name, spam_setting_new.ToString());

                return (type)spam_setting_current;
            }

            if (setting_preserialized.Contains("\"" + name + "\":"))
            {
                return (type)spam_setting_new;
            }

            return (type)spam_setting_current;
        }

        #endregion

        #region String parsing and utility functions

        /// <summary>
        /// Converts a <see cref="TwitchMessage"/> recieved from Twitch and attempts to deserialize the body in to <see cref="SpamSettings"/> object.
        /// </summary>
        private SpamSettings MessageToSetting(string body, Commands commands, SpamSetting spam_setting, out string preserialized)
        {
            int parse_start = -1;

            string chosen_setting = spam_setting.ToString();

            SpamSettings setting = null;

            chosen_setting = spam_setting.ToString();
            preserialized = string.Empty;

            //needs a colon because it's a serialized array
            if (spam_setting == SpamSetting.timeouts)
            {
                chosen_setting += ":";
            }

            //these are "top level" settings and don't require updating nested classes
            if (spam_setting == SpamSetting.permission || spam_setting == SpamSetting.enabled)
            {
                parse_start = body.IndexOf(chosen_setting);
            }
            //every other setting is a nested class within SpamSettings, need to search after the specified object
            else
            {
                parse_start = body.IndexOf(chosen_setting) + chosen_setting.Length + 1;                
            }                       

            try
            {
                body = body.Substring(parse_start);

                switch (spam_setting)
                {
                    case SpamSetting.ASCII:
                        setting = ConvertToSetting<ASCII>(body, out preserialized);
                        break;
                    case SpamSetting.Caps:
                        setting = ConvertToSetting<Caps>(body, out preserialized);
                        break;
                    case SpamSetting.Links:
                        setting = ConvertToSetting<Links>(body, out preserialized);
                        break;
                    case SpamSetting.Wall:
                        setting = ConvertToSetting<Wall>(body, out preserialized);
                        break;
                    case SpamSetting.enabled:
                        setting = ConvertToSetting<string>(body, out preserialized);
                        break;
                    case SpamSetting.permission:
                        setting = ConvertToSetting<string>(body, out preserialized);
                        break;
                    case SpamSetting.timeouts:
                        setting = ConvertToSetting<int[]>(body, out preserialized, "timeouts");
                        break;
                    default:
                        break;
                }
            }
            catch(Exception exception)
            {
                DebugBot.Error(DebugMethod.SERIALIZE, nameof(spam_settings_master), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }

            return setting;  
        }

        /// <summary>
        /// Converts the body of a <see cref="TwitchMessage"/> into a <see cref="SpamSettings"/> object with a specified type to be edited with an optional label.
        /// </summary>
        private SpamSettings ConvertToSetting<type>(string body, out string preserialized, string label = "")
        {
            preserialized = body.PreserializeAs<type>(label);

            return JsonConvert.DeserializeObject<SpamSettings>(preserialized);
        }

        /// <summary>
        /// Parses the body of a <see cref="TwitchMessage"/> for avalue to convert to a <see cref="SpamSetting"/>.
        /// Only one setting can be found at a time and it is case sensitive.
        /// Any text after the setting will be ignored.
        /// </summary>
        private SpamSetting GetSetting(string body)
        {
            string chosen_setting;

            SpamSetting spam_setting = SpamSetting.None;

            chosen_setting = body.TextBefore(" ");
                        
            if (chosen_setting.Contains("permission") || chosen_setting.Contains("enable") || chosen_setting.Contains("timeouts"))
            {
                //ignore the ":" for these settings since they are top level and not a field inside a class
                chosen_setting = body.TextBefore(":");
            }

            Enum.TryParse(chosen_setting, out spam_setting);

            return spam_setting;
        }

        #endregion
    }
}
