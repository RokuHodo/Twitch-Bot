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
        static string root = Environment.CurrentDirectory + "/JSON/Spam Filter/";

        string file_path_banned_users = root + "/Banned USers.json",
               file_path_blacklisted_words = root +  "/Blacklisted Words.json",
               file_path_settings = Environment.CurrentDirectory + "/JSON/Spam Filter/Spam Settings.json";

        List<string> banned_users,
                     blacklisted_words;

        Dictionary<string, int> timeout_tracker;

        SpamSettings master_settings;        

        public SpamFilter()
        {
            banned_users = new List<string>();
            blacklisted_words = new List<string>();
            timeout_tracker = new Dictionary<string, int>();

            Debug.BlockBegin();

            Debug.BlankLine();
            Debug.Header("Loading Banned Users");
            Debug.PrintLine("File path:", file_path_banned_users);

            LoadBannedUsers(file_path_banned_users);

            Debug.BlockEnd();
            Debug.BlockBegin();

            Debug.BlankLine();
            Debug.Header("Loading Blacklisted Words");
            Debug.PrintLine("File path:", file_path_blacklisted_words);
            Debug.BlankLine();

            LoadBlacklist(file_path_blacklisted_words);

            Debug.BlockEnd();
            Debug.BlockBegin();

            Debug.BlankLine();
            Debug.Header("Loading Spam Filter Settings");
            Debug.PrintLine("File path:", file_path_settings);
            Debug.BlankLine();

            LoadSettings(file_path_settings);

            Debug.BlockEnd();                                            
        }

        #region Load spam settings

        private void LoadBannedUsers(string file_path)
        {           
            try
            {               
                string preloaded = File.ReadAllText(file_path);

                if (!preloaded.CheckString())
                {
                    Debug.BlankLine();
                    Debug.Notify("No banned users found");

                    return;
                }

                List<string> users = JsonConvert.DeserializeObject<List<string>>(preloaded);

                if (users.Count == users.Distinct().Count())
                {
                    banned_users = users;
                }
                else
                {
                    banned_users = users.Distinct().ToList();
                }
            }
            catch(Exception exception)
            {
                Debug.Error(DebugMethod.Load, DebugObject.Banned_Users, DebugError.Exception);
                Debug.PrintLine(nameof(exception), exception.Message);
            }            
        }

        private void LoadBlacklist(string file_path)
        {
            try
            {
                string preloaded = File.ReadAllText(file_path);

                if (!preloaded.CheckString())
                {
                    Debug.BlankLine();
                    Debug.Notify("No blacklisted words found");

                    return;
                }

                List<string> words = JsonConvert.DeserializeObject<List<string>>(preloaded);

                if (words.Count == words.Distinct().Count())
                {
                    blacklisted_words = words;
                }
                else
                {
                    blacklisted_words = words.Distinct().ToList();
                }

                Debug.SubHeader("Blacklisted words");
                Debug.PrintObject(blacklisted_words);
            }
            catch (Exception exception)
            {
                Debug.Error(DebugMethod.Load, DebugObject.Blacklisted_Words, DebugError.Exception);
                Debug.PrintLine(nameof(exception), exception.Message);
            }
        }

        private void LoadSettings(string file_path)
        {
            try
            {
                string settings = File.ReadAllText(file_path);

                if (!settings.CheckString())
                {
                    Debug.BlankLine();
                    Debug.Notify("No spam settings found");

                    return;
                }

                master_settings = JsonConvert.DeserializeObject<SpamSettings>(settings);

                Debug.SubHeader("Spam settings");
                Debug.PrintObject(master_settings);
            }
            catch(Exception exception)
            {
                Debug.Error(DebugMethod.Load, DebugObject.Spam_Settings, DebugError.Exception);
                Debug.PrintLine(nameof(exception), exception.Message);
            }          
        }

        #endregion

        #region Spam checks

        public bool MessagePasses(Message message, TwitchClientOAuth bot, TwitchClientOAuth broadcaster)
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

            if (!CheckBlacklist(message, master_settings.Blacklist, blacklisted_words))
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

            string body_no_whitespace = message.body.RemoveWhiteSpace(WhiteSpace.Both);

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
            if(blacklisted_words.Count == 0)
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

            string body_no_whitespace = message.body.RemoveWhiteSpace();

            char[] body = body_no_whitespace.ToCharArray(),
                   body_uppercase = body_no_whitespace.ToUpper().ToCharArray();

            int characters_uppercase = 0;

            for (int index = 0; index < body.Length; index++)
            {
                //NOTE: this will flag any special character as an uppercase since it has no upprcase variant
                //Add a filter in the future?
                if (body[index] == body_uppercase[index])
                {
                    ++characters_uppercase;
                }
            }

            return CheckPercent(settings.percent, characters_uppercase, body.Length);
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

                Debug.Error("\"" + sender + "\" has been timed out for \"" + timeout_increments[timeout_tracker[sender]] + "\" second(s)");
                Debug.PrintLine(nameof(reason), reason);
            }
            else
            {
                if (!banned_users.Contains(sender))
                {
                    banned_users.Add(sender);

                    JsonConvert.SerializeObject(banned_users, Formatting.Indented).OverrideFile(file_path_banned_users);
                }

                bot.SendWhisper(sender, "Banned for " + reason + ".");
                broadcaster.Ban(sender, reason + " [bot]");

                Debug.Error("\"" + sender + "\" has been banned.");
                Debug.PrintLine(nameof(reason), reason);
            }

            ++timeout_tracker[sender];
        }

        #endregion

        #region Change and apply spam settings

        public void ChangeSetting(Message message, Commands commands)
        {
            Debug.BlankLine();
            Debug.SubHeader("Updating spam setting...");

            string preserialized,
                   body = commands.ParseCommandString(message);

            SpamSetting spam_setting = GetSetting(body);

            if(spam_setting == SpamSetting.None)
            {
                Debug.Error(DebugMethod.Update, DebugObject.Setting, DebugError.Null);
                Debug.PrintLine("sub setting", "null");

                return;
            }

            SpamSettings setting = MessageToSetting(body, commands, spam_setting, out preserialized);

            if(setting == null)
            {
                Debug.Error(DebugMethod.Update, DebugObject.Spam_Settings, DebugError.Null);
                Debug.PrintLine("setting", "null");

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
                        master_settings.ASCII.permission = ApplySetting<UserType>(setting.ASCII.percent, master_settings.ASCII.permission, "permission", preserialized);

                        Debug.Success(DebugMethod.Update, DebugObject.Spam_Settings, master_settings.ASCII.GetType().Name);
                        Debug.PrintObject(master_settings.ASCII);

                        break;
                    case SpamSetting.Caps:
                        master_settings.Caps.enabled = ApplySetting<bool>(setting.Caps.enabled, master_settings.Caps.enabled, "enabled", preserialized);
                        master_settings.Caps.length = ApplySetting<int>(setting.Caps.length, master_settings.Caps.length, "length", preserialized);
                        master_settings.Caps.percent = ApplySetting<int>(setting.Caps.percent, master_settings.Caps.percent, "percent", preserialized);
                        master_settings.Caps.permission = ApplySetting<UserType>(setting.Caps.percent, master_settings.Caps.permission, "permission", preserialized);

                        Debug.Success(DebugMethod.Update, DebugObject.Spam_Settings, master_settings.Caps.GetType().Name);
                        Debug.PrintObject(master_settings.Caps);

                        break;
                    case SpamSetting.Links:
                        master_settings.Links.enabled = ApplySetting<bool>(setting.Links.enabled, master_settings.Links.enabled, "enabled", preserialized);
                        master_settings.Links.permission = ApplySetting<UserType>(setting.Links.permission, master_settings.Links.permission, "permission", preserialized);

                        Debug.Success(DebugMethod.Update, DebugObject.Spam_Settings, master_settings.Links.GetType().Name);
                        Debug.PrintObject(master_settings.Links);

                        break;
                    case SpamSetting.Wall:
                        master_settings.Wall.enabled = ApplySetting<bool>(setting.Wall.enabled, master_settings.Wall.enabled, "enabled", preserialized);
                        master_settings.Wall.length = ApplySetting<int>(setting.Wall.length, master_settings.Wall.length, "length", preserialized);                        
                        master_settings.Wall.permission = ApplySetting<UserType>(setting.Wall.permission, master_settings.Wall.permission, "permission", preserialized);

                        Debug.Success(DebugMethod.Update, DebugObject.Spam_Settings, master_settings.Wall.GetType().Name);
                        Debug.PrintObject(master_settings.Wall);

                        break;
                    case SpamSetting.enabled:
                        master_settings.enabled = ApplySetting<bool>(setting.enabled, master_settings.enabled, "enabled", preserialized);

                        Debug.Success(DebugMethod.Update, DebugObject.Spam_Settings, "Master");
                        Debug.PrintLine("enabled", master_settings.enabled.ToString());

                        break;
                    case SpamSetting.permission:
                        master_settings.permission = ApplySetting<UserType>(setting.permission, master_settings.permission, "permission", preserialized);

                        Debug.Success(DebugMethod.Update, DebugObject.Spam_Settings, "Master");
                        Debug.PrintLine("permission", master_settings.permission.ToString());

                        break;
                    case SpamSetting.timeouts:
                        master_settings.timeouts = ApplySetting<int[]>(setting.timeouts, master_settings.timeouts, "timeouts", preserialized);

                        Debug.Success(DebugMethod.Update, DebugObject.Spam_Settings, nameof(master_settings.timeouts));
                        Debug.PrintObject(master_settings.timeouts);

                        break;
                    default:
                        break;
                }

                JsonConvert.SerializeObject(master_settings, Formatting.Indented).OverrideFile(file_path_settings);
            }
            catch(Exception ex)
            {
                Debug.Error(DebugMethod.Update, DebugObject.Spam_Settings, DebugError.Exception);
                Debug.PrintLine("Exception", ex.Message);
            }
        }

        private type ApplySetting<type>(object setting_new, object setting_current, string setting_name, string setting_preserialized)
        {
            if(setting_new == setting_current)
            {
                Debug.Notify("Warning: the new setting for \"" + setting_name + "\" is the same as the current setting. Setting not applied.");

                return (type)setting_current;
            }

            //"Check Syntax"
            if(setting_new is Enum)
            {
                int size = Enum.GetNames(typeof(type)).Length - 1,
                permission_value = Convert.ToInt32(setting_new);

                if (permission_value > size)
                {
                    Debug.SyntaxError(DebugObject.Spam_Settings, DebugObject.Setting, SyntaxError.EnumRange, size);
                    Debug.PrintLine(setting_name, ((int)setting_new).ToString());

                    return (type)setting_current;
                }
            }

            if(setting_new is int && (int)setting_new < 0)
            {
                Debug.SyntaxError(DebugObject.Spam_Settings, DebugObject.Setting, SyntaxError.PositiveZero);
                Debug.PrintLine(setting_name, ((int)setting_new).ToString());

                return (type)setting_current;
            }

            if (setting_preserialized.Contains("\"" + setting_name + "\":") && setting_new != null)
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

            chosen_setting = 
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
            catch(Exception ex)
            {
                Debug.Error(DebugMethod.Serialize, DebugObject.Spam_Settings, DebugError.Exception);
                Debug.PrintLine("Exception", ex.Message);
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
