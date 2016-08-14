using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using TwitchChatBot.Debugger;
using TwitchChatBot.Enums.Debug;
using TwitchChatBot.Enums.Chat;
using TwitchChatBot.Extensions;
using TwitchChatBot.Models.Bot;
using TwitchChatBot.Clients;
using TwitchChatBot.Extensions.Files;

using TwitchChatBot.Enums.Extensions;

namespace TwitchChatBot.Chat
{
    class SpamFilter
    {
        readonly string file_path_banned_users = Environment.CurrentDirectory + "/JSON/Spam Filter/Banned USers.json",
                        file_path_blacklisted_words = Environment.CurrentDirectory + "/JSON/Spam Filter/Blacklisted Words.json",
                        file_path_settings = Environment.CurrentDirectory + "/JSON/Spam Filter/Spam Settings.json";

        List<string> banned_users_list,
                     blacklisted_words_list;

        Dictionary<string, int> timeout_tracker;

        SpamSettings master_settings;        

        public SpamFilter()
        {
            banned_users_list = new List<string>();
            blacklisted_words_list = new List<string>();
            timeout_tracker = new Dictionary<string, int>();

            BotDebug.BlockBegin();

            BotDebug.BlankLine();
            BotDebug.Header("Loading Banned Users");
            BotDebug.PrintLine("File path:", file_path_banned_users);

            Load_BannedUsers(file_path_banned_users);

            BotDebug.BlockEnd();
            BotDebug.BlockBegin();

            BotDebug.BlankLine();
            BotDebug.Header("Loading Blacklisted Words");
            BotDebug.PrintLine("File path:", file_path_blacklisted_words);
            BotDebug.BlankLine();

            Load_BlacklistedWords(file_path_blacklisted_words);

            BotDebug.BlockEnd();
            BotDebug.BlockBegin();

            BotDebug.BlankLine();
            BotDebug.Header("Loading Spam Filter Settings");
            BotDebug.PrintLine("File path:", file_path_settings);
            BotDebug.BlankLine();

            Load_Settings(file_path_settings);

            BotDebug.BlockEnd();                                            
        }

        #region Load spam settings

        private void Load_BannedUsers(string file_path)
        {           
            try
            {               
                string banned_users_preloaded = File.ReadAllText(file_path);

                if (!banned_users_preloaded.CheckString())
                {
                    BotDebug.BlankLine();
                    BotDebug.Notify("No banned users found");

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
                BotDebug.Error(DebugMethod.Load, DebugObject.Banned_Users, DebugError.Exception);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }            
        }

        private void Load_BlacklistedWords(string file_path)
        {
            try
            {
                string blacklisted_words_preloaded = File.ReadAllText(file_path);

                if (!blacklisted_words_preloaded.CheckString())
                {
                    BotDebug.BlankLine();
                    BotDebug.Notify("No blacklisted words found");

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

                BotDebug.SubHeader("Blacklisted words");
                BotDebug.PrintObject(blacklisted_words_list);
            }
            catch(Exception exception)
            {
                BotDebug.Error(DebugMethod.Load, DebugObject.Blacklisted_Words, DebugError.Exception);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }
        }

        private void Load_Settings(string file_path)
        {
            try
            {
                string settings = File.ReadAllText(file_path);

                if (!settings.CheckString())
                {
                    BotDebug.BlankLine();
                    BotDebug.Notify("No spam settings found");

                    return;
                }

                master_settings = JsonConvert.DeserializeObject<SpamSettings>(settings);

                BotDebug.SubHeader("Spam settings");
                BotDebug.PrintObject(master_settings);
            }
            catch(Exception exception)
            {
                BotDebug.Error(DebugMethod.Load, DebugObject.Spam_Settings, DebugError.Exception);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }          
        }

        #endregion

        #region Spam checks

        public bool CheckMessage(Message message, TwitchClientOAuth bot, TwitchClientOAuth broadcaster)
        {
            if(master_settings == null || master_settings == default(SpamSettings))
            {
                return true;
            }

            if (!timeout_tracker.ContainsKey(message.sender.name))
            {
                timeout_tracker[message.sender.name] = 0;
            }

            if(CheckPermission(master_settings.permission, message.sender.user_type) || !master_settings.enabled)
            {
                return true;
            }

            if (!CheckASCII(message, master_settings.ASCII))
            {
                Timeout(bot, broadcaster, message.sender.name, "excessive ascii", master_settings.timeouts);

                return false;
            }

            if (!CheckBlacklist(message, master_settings.Blacklist, blacklisted_words_list))
            {
                Timeout(bot, broadcaster, message.sender.name, "use of blacklisted word(s)", master_settings.timeouts);

                return false;
            }

            if (!CheckCaps(message, master_settings.Caps))
            {
                Timeout(bot, broadcaster, message.sender.name, "excessive caps", master_settings.timeouts);

                return false;
            }

            if (!CheckLinks(message, master_settings.Links))
            {
                Timeout(bot, broadcaster, message.sender.name, "posting links", master_settings.timeouts);

                return false;
            }

            if (!CheckWall(message, master_settings.Wall))
            {
                Timeout(bot, broadcaster, message.sender.name, "wall of text", master_settings.timeouts);

                return false;
            }                               

            return true;
        }        

        private bool CheckASCII(Message message, ASCII settings)
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

            return CheckPercent(settings.percent, characters_ascii, ascii_bytes.Length);
        }

        private bool CheckBlacklist(Message message, Blacklist settings, List<string> blacklist)
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

        private bool CheckCaps(Message message, Caps settings)
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

            return CheckPercent(settings.percent, characters_uppercase, body_no_whitespace.Length);
        }

        private bool CheckLinks(Message message, Links settings)
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

        private bool CheckWall(Message message, Wall settings)
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

                BotDebug.Error("\"" + sender + "\" has been timed out for \"" + timeout_increments[timeout_tracker[sender]] + "\" second(s)");
                BotDebug.PrintLine(nameof(reason), reason);
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

                BotDebug.Error("\"" + sender + "\" has been banned.");
                BotDebug.PrintLine(nameof(reason), reason);
            }

            ++timeout_tracker[sender];
        }

        #endregion

        #region Change and apply spam settings

        public void Modify_BlacklistedWords(Commands commands, Message message)
        {
            string temp = commands.ParseCommandString(message),
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
                BotDebug.Error(DebugMethod.Modify, DebugObject.Command, DebugError.Exception);
                BotDebug.PrintLine(nameof(exception), exception.Message);
                BotDebug.PrintLine(nameof(temp), temp);
                BotDebug.PrintLine(nameof(key), key);
                BotDebug.PrintLine(nameof(message.body), message.body);
            }
        }

        private void Add_BlacklistedWord(Message message)
        {
            BotDebug.BlankLine();
            BotDebug.SubHeader("Adding blacklisted words...");

            bool list_modified = false;

            string blacklisted_words = message.body;

            string[] blacklisted_words_array;

            if (!blacklisted_words.CheckString())
            {
                BotDebug.Error(DebugMethod.Add, DebugObject.Blacklisted_Words, DebugError.Null);
                BotDebug.PrintLine(nameof(blacklisted_words), "null");

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
                        BotDebug.Error(DebugMethod.Add, DebugObject.Blacklisted_Words, DebugError.ExistYes);
                    }
                    else
                    {
                        BotDebug.Success(DebugMethod.Add, DebugObject.Blacklisted_Words, _word);

                        blacklisted_words_list.Add(_word);

                        list_modified = true;
                    }

                    BotDebug.PrintLine(nameof(word), _word);
                }

                if (list_modified)
                {
                    Notify.Success(DebugMethod.Add, DebugObject.Blacklisted_Words, string.Empty, message);

                    JsonConvert.SerializeObject(blacklisted_words_list, Formatting.Indented).OverrideFile(file_path_blacklisted_words);
                }
            }
            catch (Exception exception)
            {
                Notify.Error(DebugMethod.Add, DebugObject.Blacklisted_Words, nameof(blacklisted_words), DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Add, DebugObject.Blacklisted_Words, DebugError.Exception);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }
        }

        private void Edit_BlacklistedWord(Message message)
        {
            BotDebug.BlankLine();
            BotDebug.SubHeader("Editting blacklisted word...");

            string blacklisted_words = message.body;

            string[] blacklisted_words_array;

            if (!blacklisted_words.CheckString())
            {
                Notify.Error(DebugMethod.Edit, DebugObject.Blacklisted_Words, string.Empty, DebugError.Null, message);

                BotDebug.Error(DebugMethod.Edit, DebugObject.Blacklisted_Words, DebugError.Null);
                BotDebug.PrintLine(nameof(blacklisted_words), "null");

                return;
            }

            blacklisted_words_array = blacklisted_words.StringToArray<string>(',');

            //only allow one word to be editted at a time now 
            if (blacklisted_words_array.Length != 2)
            {
                BotDebug.SyntaxError(DebugObject.Blacklisted_Words, DebugObject.Blacklisted_Words, SyntaxError.ArrayLength);
                BotDebug.PrintLine(nameof(blacklisted_words_array.Length), blacklisted_words_array.Length.ToString());
                BotDebug.PrintLine("required length", "2");

                Notify.Error(DebugMethod.Edit, DebugObject.Blacklisted_Words, blacklisted_words_array[0], DebugError.Syntax, message);

                BotDebug.Error(DebugMethod.Edit, DebugObject.Blacklisted_Words, DebugError.Syntax);

                return;
            }

            if (!blacklisted_words_list.Contains(blacklisted_words_array[0]))
            {
                Notify.Error(DebugMethod.Edit, DebugObject.Blacklisted_Words, blacklisted_words_array[0], DebugError.ExistNo, message);

                BotDebug.Error(DebugMethod.Edit, DebugObject.Blacklisted_Words, DebugError.ExistNo);
                BotDebug.PrintLine("word", blacklisted_words_array[0]);

                return;
            }

            try
            {
                blacklisted_words_list.Remove(blacklisted_words_array[0]);
                blacklisted_words_list.Add(blacklisted_words_array[1].RemoveWhiteSpace());

                JsonConvert.SerializeObject(blacklisted_words_list, Formatting.Indented).OverrideFile(file_path_blacklisted_words);

                Notify.Success(DebugMethod.Edit, DebugObject.Blacklisted_Words, blacklisted_words_array[0] + " -> " + blacklisted_words_array[1], message);

                BotDebug.Success(DebugMethod.Edit, DebugObject.Blacklisted_Words, blacklisted_words_array[0]);
                BotDebug.PrintLine("old word", blacklisted_words_array[0]);
                BotDebug.PrintLine("new word", blacklisted_words_array[1].RemoveWhiteSpace());
            }
            catch (Exception exception)
            {
                Notify.Error(DebugMethod.Edit, DebugObject.Blacklisted_Words, blacklisted_words_array[0], DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Edit, DebugObject.Blacklisted_Words, DebugError.Exception);
                BotDebug.PrintLine(nameof(exception), exception.Message);

                BotDebug.BlankLine();
                BotDebug.PrintLine("Array:");
                BotDebug.PrintObject(blacklisted_words_array);
            }
        }

        private void Remove_BlacklistedWord(Message message)
        {
            BotDebug.BlankLine();
            BotDebug.SubHeader("Removing blacklisted words...");

            bool list_modified = false;

            string blacklisted_words = message.body;

            string[] blacklisted_words_array;

            if (!blacklisted_words.CheckString())
            {
                BotDebug.Error(DebugMethod.Remove, DebugObject.Blacklisted_Words, DebugError.Null);
                BotDebug.PrintLine(nameof(blacklisted_words), "null");

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
                        BotDebug.Error(DebugMethod.Remove, DebugObject.Blacklisted_Words, DebugError.ExistNo);
                    }
                    else
                    {
                        BotDebug.Success(DebugMethod.Remove, DebugObject.Blacklisted_Words, _word);

                        blacklisted_words_list.Remove(_word);

                        list_modified = true;
                    }

                    BotDebug.PrintLine(nameof(word), _word);
                }

                if (list_modified)
                {
                    Notify.Success(DebugMethod.Remove, DebugObject.Blacklisted_Words, string.Empty, message);

                    JsonConvert.SerializeObject(blacklisted_words_list, Formatting.Indented).OverrideFile(file_path_blacklisted_words);
                }
            }
            catch (Exception exception)
            {
                Notify.Error(DebugMethod.Remove, DebugObject.Blacklisted_Words, nameof(blacklisted_words), DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Remove, DebugObject.Blacklisted_Words, DebugError.Exception);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }
        }             

        public void ChangeSetting(Message message, Commands commands)
        {
            BotDebug.BlankLine();
            BotDebug.SubHeader("Updating spam setting...");

            bool updated = false;

            string preserialized,
                   body = commands.ParseCommandString(message);

            SpamSetting spam_setting = GetSetting(body);

            if(spam_setting == SpamSetting.None)
            {
                Notify.Error(DebugMethod.Update, DebugObject.Spam_Settings, spam_setting.ToString(), DebugError.Null, message);

                BotDebug.Error(DebugMethod.Update, DebugObject.Setting, DebugError.Null);
                BotDebug.PrintLine("sub setting", "null");

                return;
            }

            SpamSettings setting = MessageToSetting(body, commands, spam_setting, out preserialized);

            if(setting == null)
            {
                Notify.Error(DebugMethod.Update, DebugObject.Spam_Settings, string.Empty, DebugError.Null, message);

                BotDebug.Error(DebugMethod.Update, DebugObject.Spam_Settings, DebugError.Null);
                BotDebug.PrintLine("setting", "null");

                return;
            }

            try
            {
                switch (spam_setting)
                {
                    case SpamSetting.ASCII:
                        master_settings.ASCII.enabled = ApplySetting<bool>(setting.ASCII.enabled, master_settings.ASCII.enabled, "enabled", preserialized);
                        master_settings.ASCII.length = ApplySetting<int>(setting.ASCII.length, master_settings.ASCII.length, "length", preserialized);
                        master_settings.ASCII.percent = ApplySetting<int>(setting.ASCII.percent, master_settings.ASCII.percent, "percent", preserialized);
                        master_settings.ASCII.permission = ApplySetting<UserType>(setting.ASCII.permission, master_settings.ASCII.permission, "permission", preserialized);                                               

                        BotDebug.Success(DebugMethod.Update, DebugObject.Spam_Settings, nameof(master_settings.ASCII));
                        BotDebug.PrintObject(master_settings.ASCII);

                        updated = true;

                        break;
                    case SpamSetting.Caps:
                        master_settings.Caps.enabled = ApplySetting<bool>(setting.Caps.enabled, master_settings.Caps.enabled, "enabled", preserialized);
                        master_settings.Caps.length = ApplySetting<int>(setting.Caps.length, master_settings.Caps.length, "length", preserialized);
                        master_settings.Caps.percent = ApplySetting<int>(setting.Caps.percent, master_settings.Caps.percent, "percent", preserialized);
                        master_settings.Caps.permission = ApplySetting<UserType>(setting.Caps.percent, master_settings.Caps.permission, "permission", preserialized);

                        BotDebug.Success(DebugMethod.Update, DebugObject.Spam_Settings, nameof(master_settings.Caps));
                        BotDebug.PrintObject(master_settings.Caps);

                        updated = true;

                        break;
                    case SpamSetting.Links:
                        master_settings.Links.enabled = ApplySetting<bool>(setting.Links.enabled, master_settings.Links.enabled, "enabled", preserialized);
                        master_settings.Links.permission = ApplySetting<UserType>(setting.Links.permission, master_settings.Links.permission, "permission", preserialized);

                        BotDebug.Success(DebugMethod.Update, DebugObject.Spam_Settings, nameof(master_settings.Links));
                        BotDebug.PrintObject(master_settings.Links);

                        updated = true;

                        break;
                    case SpamSetting.Wall:
                        master_settings.Wall.enabled = ApplySetting<bool>(setting.Wall.enabled, master_settings.Wall.enabled, "enabled", preserialized);
                        master_settings.Wall.length = ApplySetting<int>(setting.Wall.length, master_settings.Wall.length, "length", preserialized);                        
                        master_settings.Wall.permission = ApplySetting<UserType>(setting.Wall.permission, master_settings.Wall.permission, "permission", preserialized);

                        BotDebug.Success(DebugMethod.Update, DebugObject.Spam_Settings, nameof(master_settings.Wall));
                        BotDebug.PrintObject(master_settings.Wall);

                        updated = true;

                        break;
                    case SpamSetting.enabled:
                        master_settings.enabled = ApplySetting<bool>(setting.enabled, master_settings.enabled, "enabled", preserialized);

                        BotDebug.Success(DebugMethod.Update, DebugObject.Spam_Settings, "Master");
                        BotDebug.PrintLine("enabled", master_settings.enabled.ToString());

                        updated = true;

                        break;
                    case SpamSetting.permission:
                        master_settings.permission = ApplySetting<UserType>(setting.permission, master_settings.permission, "permission", preserialized);

                        BotDebug.Success(DebugMethod.Update, DebugObject.Spam_Settings, "Master");
                        BotDebug.PrintLine("permission", master_settings.permission.ToString());

                        updated = true;

                        break;
                    case SpamSetting.timeouts:
                        master_settings.timeouts = ApplySetting<int[]>(setting.timeouts, master_settings.timeouts, "timeouts", preserialized);

                        BotDebug.Success(DebugMethod.Update, DebugObject.Spam_Settings, nameof(master_settings.timeouts));
                        BotDebug.PrintObject(master_settings.timeouts);

                        updated = true;

                        break;
                    default:
                        break;
                }

                if (updated)
                {
                    Notify.Success(DebugMethod.Update, DebugObject.Spam_Settings, spam_setting.ToString(), message);
                }               

                JsonConvert.SerializeObject(master_settings, Formatting.Indented).OverrideFile(file_path_settings);
            }
            catch(Exception exception)
            {
                Notify.Error(DebugMethod.Update, DebugObject.Spam_Settings, spam_setting.ToString(), DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Update, DebugObject.Spam_Settings, DebugError.Exception);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }
        }

        private type ApplySetting<type>(object setting_new, object setting_current, string name, string setting_preserialized)
        {
            if(setting_new == setting_current)
            {
                BotDebug.Notify("Warning: the new setting for \"" + name + "\" is the same as the current setting. Setting not applied.");

                return (type)setting_current;
            }

            if(setting_new is Enum)
            {
                int size = Enum.GetNames(typeof(type)).Length - 1,
                permission_value = Convert.ToInt32(setting_new);

                if (permission_value > size)
                {
                    BotDebug.SyntaxError(DebugObject.Spam_Settings, DebugObject.Setting, SyntaxError.EnumRange, size);
                    BotDebug.PrintLine(name, setting_new.ToString());

                    return (type)setting_current;
                }
            }

            if(setting_new is int && (int)setting_new < 0)
            {
                BotDebug.SyntaxError(DebugObject.Spam_Settings, DebugObject.Setting, SyntaxError.PositiveZero);
                BotDebug.PrintLine(name, ((int)setting_new).ToString());

                return (type)setting_current;
            }

            if (setting_preserialized.Contains("\"" + name + "\":"))
            {
                return (type)setting_new;
            }

            return (type)setting_current;
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
                BotDebug.Error(DebugMethod.Serialize, DebugObject.Spam_Settings, DebugError.Exception);
                BotDebug.PrintLine(nameof(exception), exception.Message);
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

        private bool CheckPercent(int allowable_percent, int amount_fail, int array_size)
        {
            int percent = 100 * amount_fail / array_size;

            if (allowable_percent > Convert.ToInt32(percent))
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
