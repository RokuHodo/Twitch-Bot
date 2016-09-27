using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using TwitchBot.Debugger;
using TwitchBot.Enums.Chat;
using TwitchBot.Extensions;
using TwitchBot.Helpers;
using TwitchBot.Models.Bot.Spam;
using TwitchBot.Clients;
using TwitchBot.Extensions.Files;

using TwitchBot.Enums.Extensions;

namespace TwitchBot.Chat
{
    class SpamFilter
    {
        readonly string file_path_banned_users = Environment.CurrentDirectory + "/JSON/Spam Filter/Banned USers.json",
                        file_path_blacklisted_words = Environment.CurrentDirectory + "/JSON/Spam Filter/Blacklisted Words.json",
                        file_path_settings = Environment.CurrentDirectory + "/JSON/Spam Filter/Spam Settings.json";

        List<string> banned_users_list,
                     blacklisted_words_list;

        Dictionary<string, int> timeout_tracker;

        SpamSettings spam_settings_master;        

        public SpamFilter()
        {
            banned_users_list = new List<string>();
            blacklisted_words_list = new List<string>();
            timeout_tracker = new Dictionary<string, int>();

            DebugBot.BlockBegin();

            DebugBot.BlankLine();
            DebugBot.Header("Loading Banned Users");
            DebugBot.PrintLine("File path:", file_path_banned_users);

            Load_BannedUsers(file_path_banned_users);

            DebugBot.BlockEnd();
            DebugBot.BlockBegin();

            DebugBot.BlankLine();
            DebugBot.Header("Loading Blacklisted Words");
            DebugBot.PrintLine("File path:", file_path_blacklisted_words);
            DebugBot.BlankLine();

            Load_BlacklistedWords(file_path_blacklisted_words);

            DebugBot.BlockEnd();
            DebugBot.BlockBegin();

            DebugBot.BlankLine();
            DebugBot.Header("Loading Spam Filter Settings");
            DebugBot.PrintLine("File path:", file_path_settings);
            DebugBot.BlankLine();

            Load_Settings(file_path_settings);

            DebugBot.BlockEnd();                                            
        }

        #region Load spam settings

        private void Load_BannedUsers(string file_path)
        {           
            try
            {               
                string banned_users_preloaded = File.ReadAllText(file_path);

                if (!banned_users_preloaded.CheckString())
                {
                    DebugBot.BlankLine();
                    DebugBot.PrintLine(DebugMessageType.WARNING, "No banned users found");

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
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.LOAD, nameof(banned_users_list), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }            
        }

        private void Load_BlacklistedWords(string file_path)
        {
            try
            {
                string blacklisted_words_preloaded = File.ReadAllText(file_path);

                if (!blacklisted_words_preloaded.CheckString())
                {
                    DebugBot.BlankLine();
                    DebugBot.PrintLine(DebugMessageType.WARNING, "No blacklisted words found");

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
                DebugBot.PrintLine(DebugMessageType.ERROR,DebugMethod.LOAD, nameof(blacklisted_words_list), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        private void Load_Settings(string file_path)
        {
            try
            {
                string settings = File.ReadAllText(file_path);

                if (!settings.CheckString())
                {
                    DebugBot.BlankLine();
                    DebugBot.PrintLine(DebugMessageType.WARNING, "No spam settings found");

                    return;
                }

                spam_settings_master = JsonConvert.DeserializeObject<SpamSettings>(settings);

                DebugBot.SubHeader("Spam settings");
                DebugBot.PrintObject(spam_settings_master);
            }
            catch(Exception exception)
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.LOAD, nameof(spam_settings_master), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }          
        }

        #endregion

        #region Spam checks

        public bool CheckMessage(TwitchMessage message, TwitchClientOAuth bot, TwitchClientOAuth broadcaster)
        {
            if(spam_settings_master == null || spam_settings_master == default(SpamSettings))
            {
                return true;
            }

            if (!timeout_tracker.ContainsKey(message.sender.name))
            {
                timeout_tracker[message.sender.name] = 0;
            }

            if(CheckPermission(spam_settings_master.permission, message.sender.user_type) || !spam_settings_master.enabled)
            {
                return true;
            }

            if (!CheckASCII(message, spam_settings_master.ASCII))
            {
                Timeout(bot, broadcaster, message.sender.name, "excessive ascii", spam_settings_master.timeouts);

                return false;
            }

            if (!CheckBlacklist(message, spam_settings_master.Blacklist, blacklisted_words_list))
            {
                Timeout(bot, broadcaster, message.sender.name, "use of blacklisted word(s)", spam_settings_master.timeouts);

                return false;
            }

            if (!CheckCaps(message, spam_settings_master.Caps))
            {
                Timeout(bot, broadcaster, message.sender.name, "excessive caps", spam_settings_master.timeouts);

                return false;
            }

            if (!CheckLinks(message, spam_settings_master.Links))
            {
                Timeout(bot, broadcaster, message.sender.name, "posting links", spam_settings_master.timeouts);

                return false;
            }

            if (!CheckWall(message, spam_settings_master.Wall))
            {
                Timeout(bot, broadcaster, message.sender.name, "wall of text", spam_settings_master.timeouts);

                return false;
            }                               

            return true;
        }        

        private bool CheckASCII(TwitchMessage message, ASCII settings)
        {
            if (CheckPermission(settings.permission, message.sender.user_type) || !settings.enabled)
            {
                return true;
            }

            if (message.body.Length < settings.length)
            {
                return true;
            }

            int characters_ascii = 0;

            string body_no_whitespace = message.body.RemoveWhiteSpace(WhiteSpace.All);

            if (body_no_whitespace.Length < settings.length)
            {
                return true;
            }

            byte[] ascii_bytes = Encoding.GetEncoding(437).GetBytes(body_no_whitespace.ToCharArray());

            foreach (byte _byte in ascii_bytes)
            {
                if (_byte > 175 && _byte < 224 || _byte == 254)
                {
                    ++characters_ascii;
                }
            }

            return characters_ascii.CheckPercent(ascii_bytes.Length, settings.percent);
        }

        private bool CheckBlacklist(TwitchMessage message, Blacklist settings, List<string> blacklist)
        {
            if(blacklisted_words_list.Count == 0)
            {
                return true;
            }

            string error = string.Empty;

            string[] words = message.body.ToLower().StringToArray<string>(' ');

            if (CheckPermission(settings.permission, message.sender.user_type) || !settings.enabled)
            {
                return true;
            }

            Match match;

            foreach (string blacklisted_word in blacklist)
            {
                if (blacklisted_word.StartsWith("*"))
                {
                    string _blacklisted_word = blacklisted_word.Substring(1).ToLower();

                    if (message.body.Contains(_blacklisted_word))
                    {
                        return false;
                    }
                }
                else
                {
                    match = Regex.Match(message.body, @"\b" + blacklisted_word + @"\b", RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool CheckCaps(TwitchMessage message, Caps settings)
        {
            if (CheckPermission(settings.permission, message.sender.user_type) || !settings.enabled)
            {
                return true;
            }

            if (message.body.Length < settings.length)
            {
                return true;
            }
            
            int characters_uppercase = 0;

            string body_no_whitespace = message.body.RemoveWhiteSpace(WhiteSpace.All);

            byte[] ascii_bytes = Encoding.ASCII.GetBytes(body_no_whitespace.ToCharArray());

            //this method only supports english for now
            foreach(byte _byte in ascii_bytes)
            {
                if(_byte > 64 && _byte < 91)
                {
                    ++characters_uppercase;
                }
            }

            return characters_uppercase.CheckPercent(body_no_whitespace.Length, settings.percent);
        }

        private bool CheckLinks(TwitchMessage message, Links settings)
        {
            if (CheckPermission(settings.permission, message.sender.user_type) || !settings.enabled)
            {
                return true;
            }

            MatchCollection matches = Regex.Matches(message.body, @"([a-zA-Z0-9]+)\.([a-zA-z]{2,})", RegexOptions.IgnoreCase);

            if(matches.Count > 0)
            {
                return false;
            }

            return true;
        }

        private bool CheckWall(TwitchMessage message, Wall settings)
        {
            if (CheckPermission(settings.permission, message.sender.user_type) || !settings.enabled)
            {
                return true;
            }

            if (message.body.Length < settings.length)
            {
                return true;
            }

            string body = message.body.RemoveWhiteSpace();

            if(body.Length > settings.length)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Spam timeouts

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

                    JsonConvert.SerializeObject(banned_users_list, Formatting.Indented).OverrideFile(file_path_banned_users);
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
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.MODIFY, nameof(blacklisted_words_list), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
                DebugBot.PrintLine(nameof(temp), temp);
                DebugBot.PrintLine(nameof(key), key);
                DebugBot.PrintLine(nameof(message.body), message.body);
            }
        }

        private void Add_BlacklistedWord(TwitchMessage message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Adding blacklisted words...");

            bool list_modified = false;

            string blacklisted_words = message.body;

            string[] blacklisted_words_array;

            if (!blacklisted_words.CheckString())
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.ADD, nameof(blacklisted_words_list), DebugError.NORMAL_NULL);
                DebugBot.PrintLine(nameof(blacklisted_words), "null");

                return;
            }

            blacklisted_words_array = blacklisted_words.StringToArray<string>(',');

            try
            {
                foreach (string word in blacklisted_words_array)
                {
                    string _word = word.RemoveWhiteSpace();

                    if (blacklisted_words_list.Contains(_word))
                    {
                        DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.ADD, nameof(blacklisted_words_list), DebugError.NORMAL_EXISTS_YES);
                    }
                    else
                    {
                        DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.ADD, nameof(blacklisted_words_list));

                        blacklisted_words_list.Add(_word);

                        list_modified = true;
                    }

                    DebugBot.PrintLine(nameof(word), _word);
                }

                if (list_modified)
                {
                    JsonConvert.SerializeObject(blacklisted_words_list, Formatting.Indented).OverrideFile(file_path_blacklisted_words);

                    Notify.Enqueue(DebugMessageType.SUCCESS, DebugMethod.ADD, message, "blacklisted word(s)");                  
                }
                else
                {
                    Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.ADD, message, "blacklisted word(s)", "no new blacklisted words found");
                }
            }
            catch (Exception exception)
            {
                Notify.Enqueue(DebugMessageType.SUCCESS, DebugMethod.ADD, message, "blacklisted word(s)", string.Empty, DebugError.NORMAL_EXCEPTION);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.ADD, nameof(blacklisted_words_list), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        private void Edit_BlacklistedWord(TwitchMessage message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Editting blacklisted word...");

            string blacklisted_words = message.body;

            string[] blacklisted_words_array;

            if (!blacklisted_words.CheckString())
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.EDIT, message, "blacklisted word(s)", string.Empty, DebugError.NORMAL_NULL);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.EDIT, nameof(blacklisted_words_list), DebugError.NORMAL_NULL);
                DebugBot.PrintLine(nameof(blacklisted_words), "null");

                return;
            }

            blacklisted_words_array = blacklisted_words.StringToArray<string>(',');

            //only allow one word to be editted at a time now 
            if (blacklisted_words_array.Length != 2)
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.EDIT, message, "blacklisted word(s)", blacklisted_words_array[0], DebugError.SYNTAX_LENGTH);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.EDIT, nameof(blacklisted_words_array), DebugError.SYNTAX_LENGTH);
                DebugBot.PrintLine(nameof(blacklisted_words_array.Length), blacklisted_words_array.Length.ToString());
                DebugBot.PrintLine("required length", "2");

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.EDIT, nameof(blacklisted_words_list), DebugError.NORMAL_SYNTAX);

                return;
            }

            if (!blacklisted_words_list.Contains(blacklisted_words_array[0]))
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.EDIT, message, "blacklisted word(s)", blacklisted_words_array[0], DebugError.NORMAL_EXISTS_NO);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.EDIT, nameof(blacklisted_words_list), DebugError.NORMAL_EXISTS_NO);
                DebugBot.PrintLine("word", blacklisted_words_array[0]);

                return;
            }

            try
            {
                blacklisted_words_list.Remove(blacklisted_words_array[0]);
                blacklisted_words_list.Add(blacklisted_words_array[1].RemoveWhiteSpace());

                JsonConvert.SerializeObject(blacklisted_words_list, Formatting.Indented).OverrideFile(file_path_blacklisted_words);

                Notify.Enqueue(DebugMessageType.SUCCESS, DebugMethod.EDIT, message, "blacklisted word(s)", blacklisted_words_array[0] + " -> " + blacklisted_words_array[1], DebugError.SYNTAX_LENGTH);

                DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.EDIT, nameof(blacklisted_words_list));
                DebugBot.PrintLine("old word", blacklisted_words_array[0]);
                DebugBot.PrintLine("new word", blacklisted_words_array[1].RemoveWhiteSpace());
            }
            catch (Exception exception)
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.EDIT, message, "blacklisted word(s)", blacklisted_words_array[0], DebugError.NORMAL_EXCEPTION);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.EDIT, nameof(blacklisted_words_list), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        private void Remove_BlacklistedWord(TwitchMessage message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Removing blacklisted words...");

            bool list_modified = false;

            string blacklisted_words = message.body;

            string[] blacklisted_words_array;

            if (!blacklisted_words.CheckString())
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.REMOVE, nameof(blacklisted_words_list), DebugError.NORMAL_NULL);
                DebugBot.PrintLine(nameof(blacklisted_words), "null");

                return;
            }

            blacklisted_words_array = blacklisted_words.StringToArray<string>(',');

            try
            {
                foreach (string word in blacklisted_words_array)
                {
                    string _word = word.RemoveWhiteSpace();

                    if (!blacklisted_words_list.Contains(_word))
                    {
                        DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.REMOVE, nameof(blacklisted_words_list), DebugError.NORMAL_EXISTS_NO);
                    }
                    else
                    {                       
                        blacklisted_words_list.Remove(_word);

                        DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.REMOVE, nameof(blacklisted_words_list));
                        DebugBot.PrintLine(nameof(word), _word);

                        list_modified = true;
                    }

                    DebugBot.PrintLine(nameof(word), _word);
                }

                if (list_modified)
                {
                    Notify.Enqueue(DebugMessageType.SUCCESS, DebugMethod.REMOVE, message, "blacklisted word(s)");

                    JsonConvert.SerializeObject(blacklisted_words_list, Formatting.Indented).OverrideFile(file_path_blacklisted_words);
                }
            }
            catch (Exception exception)
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.REMOVE, message, "blacklisted word(s)", string.Empty, DebugError.NORMAL_EXCEPTION);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.REMOVE, nameof(blacklisted_words_list), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }             

        public void ChangeSetting(TwitchMessage message, Commands commands)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Updating spam setting...");

            bool updated = false;

            string preserialized,
                   body = commands.ParseAfterCommand(message);

            SpamSetting spam_setting = GetSetting(body);

            if(spam_setting == SpamSetting.None)
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.UPDATE, message, "spam setting", spam_setting.ToString(), DebugError.NORMAL_NULL);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.UPDATE, nameof(spam_settings_master), DebugError.NORMAL_NULL);
                DebugBot.PrintLine(nameof(spam_setting), "null");

                return;
            }

            SpamSettings setting = MessageToSetting(body, commands, spam_setting, out preserialized);

            if(setting == null)
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.UPDATE, message, "spam setting", string.Empty, DebugError.NORMAL_NULL);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.UPDATE, nameof(spam_settings_master), DebugError.NORMAL_NULL);
                DebugBot.PrintLine(nameof(setting), "null");

                return;
            }

            try
            {
                switch (spam_setting)
                {
                    case SpamSetting.ASCII:
                        spam_settings_master.ASCII.enabled = ApplySetting<bool>(setting.ASCII.enabled, spam_settings_master.ASCII.enabled, "enabled", preserialized);
                        spam_settings_master.ASCII.length = ApplySetting<int>(setting.ASCII.length, spam_settings_master.ASCII.length, "length", preserialized);
                        spam_settings_master.ASCII.percent = ApplySetting<int>(setting.ASCII.percent, spam_settings_master.ASCII.percent, "percent", preserialized);
                        spam_settings_master.ASCII.permission = ApplySetting<UserType>(setting.ASCII.permission, spam_settings_master.ASCII.permission, "permission", preserialized);                                               

                        DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.UPDATE, nameof(spam_settings_master));
                        DebugBot.PrintLine(nameof(spam_setting), nameof(spam_settings_master.ASCII));
                        DebugBot.PrintObject(spam_settings_master.ASCII);

                        updated = true;

                        break;
                    case SpamSetting.Caps:
                        spam_settings_master.Caps.enabled = ApplySetting<bool>(setting.Caps.enabled, spam_settings_master.Caps.enabled, "enabled", preserialized);
                        spam_settings_master.Caps.length = ApplySetting<int>(setting.Caps.length, spam_settings_master.Caps.length, "length", preserialized);
                        spam_settings_master.Caps.percent = ApplySetting<int>(setting.Caps.percent, spam_settings_master.Caps.percent, "percent", preserialized);
                        spam_settings_master.Caps.permission = ApplySetting<UserType>(setting.Caps.percent, spam_settings_master.Caps.permission, "permission", preserialized);

                        DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.UPDATE, nameof(spam_settings_master));
                        DebugBot.PrintLine(nameof(spam_setting), nameof(spam_settings_master.Caps));
                        DebugBot.PrintObject(spam_settings_master.Caps);

                        updated = true;

                        break;
                    case SpamSetting.Links:
                        spam_settings_master.Links.enabled = ApplySetting<bool>(setting.Links.enabled, spam_settings_master.Links.enabled, "enabled", preserialized);
                        spam_settings_master.Links.permission = ApplySetting<UserType>(setting.Links.permission, spam_settings_master.Links.permission, "permission", preserialized);

                        DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.UPDATE, nameof(spam_settings_master));
                        DebugBot.PrintLine(nameof(spam_setting), nameof(spam_settings_master.Links));
                        DebugBot.PrintObject(spam_settings_master.Links);

                        updated = true;

                        break;
                    case SpamSetting.Wall:
                        spam_settings_master.Wall.enabled = ApplySetting<bool>(setting.Wall.enabled, spam_settings_master.Wall.enabled, "enabled", preserialized);
                        spam_settings_master.Wall.length = ApplySetting<int>(setting.Wall.length, spam_settings_master.Wall.length, "length", preserialized);                        
                        spam_settings_master.Wall.permission = ApplySetting<UserType>(setting.Wall.permission, spam_settings_master.Wall.permission, "permission", preserialized);

                        DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.UPDATE, nameof(spam_settings_master));
                        DebugBot.PrintLine(nameof(spam_setting), nameof(spam_settings_master.Wall));
                        DebugBot.PrintObject(spam_settings_master.Wall);

                        updated = true;

                        break;
                    case SpamSetting.enabled:
                        spam_settings_master.enabled = ApplySetting<bool>(setting.enabled, spam_settings_master.enabled, "enabled", preserialized);

                        DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.UPDATE, nameof(spam_settings_master));
                        DebugBot.PrintLine(nameof(spam_setting), nameof(spam_settings_master.enabled));
                        DebugBot.PrintLine(nameof(spam_settings_master.enabled), spam_settings_master.enabled.ToString());

                        updated = true;

                        break;
                    case SpamSetting.permission:
                        spam_settings_master.permission = ApplySetting<UserType>(setting.permission, spam_settings_master.permission, "permission", preserialized);

                        DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.UPDATE, nameof(spam_settings_master));
                        DebugBot.PrintLine(nameof(spam_setting), nameof(spam_settings_master.permission));
                        DebugBot.PrintLine(nameof(spam_settings_master.permission), spam_settings_master.permission.ToString());

                        updated = true;

                        break;
                    case SpamSetting.timeouts:
                        spam_settings_master.timeouts = ApplySetting<int[]>(setting.timeouts, spam_settings_master.timeouts, "timeouts", preserialized);

                        DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.UPDATE, nameof(spam_settings_master));
                        DebugBot.PrintLine(nameof(spam_settings_master.timeouts));
                        DebugBot.PrintObject(spam_settings_master.timeouts);

                        updated = true;

                        break;
                    default:
                        break;
                }

                if (updated)
                {
                    Notify.Enqueue(DebugMessageType.SUCCESS, DebugMethod.UPDATE, message, "spam setting", spam_setting.ToString());
                }               

                JsonConvert.SerializeObject(spam_settings_master, Formatting.Indented).OverrideFile(file_path_settings);
            }
            catch(Exception exception)
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.UPDATE, message, "spam settings", spam_setting.ToString(), DebugError.NORMAL_EXCEPTION);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.UPDATE, nameof(spam_settings_master), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        private type ApplySetting<type>(object spam_setting_new, object spam_setting_current, string name, string setting_preserialized)
        {
            if(spam_setting_new == spam_setting_current)
            {
                DebugBot.PrintLine(DebugMessageType.WARNING, "Warning: the new setting for \"" + name + "\" is the same as the current setting. Setting not applied.");

                return (type)spam_setting_current;
            }

            if(spam_setting_new is Enum)
            {
                int size = Enum.GetNames(typeof(type)).Length - 1,
                permission_value = Convert.ToInt32(spam_setting_new);

                if (permission_value > size)
                {
                    DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.APPLY, nameof(spam_setting_new), DebugError.SYNTAX_OUR_OF_BOUNDS);
                    DebugBot.PrintLine(nameof(spam_setting_new), spam_setting_new.ToString());
                    DebugBot.PrintLine(nameof(permission_value), permission_value.ToString());
                    DebugBot.PrintLine(nameof(size), size.ToString());

                    return (type)spam_setting_current;
                }
            }

            if(spam_setting_new is int && (int)spam_setting_new < 0)
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.APPLY, nameof(spam_setting_new), DebugError.SYNTAX_POSITIVE_YES);
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

        private SpamSettings MessageToSetting(string body, Commands commands, SpamSetting spam_setting, out string preserialized)
        {
            int parse_start = -1;

            string chosen_setting = spam_setting.ToString();

            SpamSettings setting = null;

            chosen_setting = spam_setting.ToString();
            preserialized = string.Empty;

            if (spam_setting == SpamSetting.timeouts)
            {
                chosen_setting += ":";
            }

            if (spam_setting == SpamSetting.permission || spam_setting == SpamSetting.enabled)// || spam_setting == SpamSetting.timeouts)
            {
                parse_start = body.IndexOf(chosen_setting);
            }
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
                        setting = ConvertToSetting<int[]>("timeouts", body, out preserialized);
                        break;
                    default:
                        break;
                }
            }
            catch(Exception exception)
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.SERIALIZE, nameof(spam_settings_master), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }

            return setting;  
        }

        private SpamSettings ConvertToSetting<type>(string body, out string preserialized)
        {
            preserialized = body.PreserializeAs<type>();
            
            return JsonConvert.DeserializeObject<SpamSettings>(preserialized);
        }

        private SpamSettings ConvertToSetting<type>(string label, string body, out string preserialized)
        {
            preserialized = body.PreserializeAs<type>(label);

            return JsonConvert.DeserializeObject<SpamSettings>(preserialized);
        }

        private SpamSetting GetSetting(string body)
        {
            string chosen_setting;

            SpamSetting spam_setting;

            int space = body.IndexOf(' ');

            if (space == -1)
            {
                return SpamSetting.None;
            }

            chosen_setting = body.Substring(0, space);

            if (chosen_setting.Contains("permission") || chosen_setting.Contains("enable") || chosen_setting.Contains("timeouts"))
            {
                chosen_setting = chosen_setting.Substring(0, chosen_setting.Length - 1);
            }

            if (!Enum.TryParse(chosen_setting, out spam_setting))
            {
                return SpamSetting.None;
            }

            return spam_setting;
        }

        private bool CheckPermission(UserType permission, UserType user_type)
        {
            if (permission > user_type)
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
