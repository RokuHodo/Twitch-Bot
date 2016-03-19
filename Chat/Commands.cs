using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using TwitchChatBot.Enums;
using TwitchChatBot.Enums.Debugger;
using TwitchChatBot.Clients;
using TwitchChatBot.Debugger;
using TwitchChatBot.Extensions;
using TwitchChatBot.Extensions.Files;

namespace TwitchChatBot.Chat
{
    class Commands
    {
        //where everything is loaded from
        string file_path_normal = Environment.CurrentDirectory + "/Commands/Normal.txt";
        string file_path_permanent = Environment.CurrentDirectory + "/Commands/Permanent.txt";

        //dictionaries to load the commands
        Dictionary<string, Command> commands = new Dictionary<string, Command>();
        Dictionary<string, string> preloaded_commands_normal = new Dictionary<string, string>();
        Dictionary<string, string> preloaded_commands_permanent = new Dictionary<string, string>();

        public Commands(Variables variables)
        {
            //preload and load the permanent commands (cannot be removed)

            Debug.BlankLine();
            Debug.Header("Preloading permanent commands");
            Debug.SubText("File path: " + file_path_permanent + Environment.NewLine);

            preloaded_commands_permanent = PreLoad(File.ReadAllLines(file_path_permanent));

            Debug.BlankLine();
            Debug.Header("Loading permanent commands" + Environment.NewLine);

            foreach (KeyValuePair<string, string> pair in preloaded_commands_permanent)
            {
                Load(pair.Key, pair.Value, true, variables);
            }

            //preload and load the normal commands (can be removed)
            Debug.BlankLine();
            Debug.Header("Preloading normal commands");
            Debug.SubText("File path: " + file_path_normal + Environment.NewLine);

            preloaded_commands_normal = PreLoad(File.ReadAllLines(file_path_normal));

            Debug.BlankLine();
            Debug.Header("Loading normal commands" + Environment.NewLine);

            foreach (KeyValuePair<string, string> pair in preloaded_commands_normal)
            {
                Load(pair.Key, pair.Value, false, variables);
            }
        }

        #region Load commands

        /// <summary>
        /// Loops through an array of strings and returns the elements that contain more than one word into a <see cref="Dictionary{TKey, TValue}"/> on launch.
        /// The first word is the "key" and anything after is the "value".
        /// Commented lines and whitespace lines are ignored. 
        /// </summary>
        /// <param name="lines">Array of strings to be processed.</param>
        /// <returns></returns>
        private Dictionary<string, string> PreLoad(string[] lines)
        {
            string key, value;

            Dictionary<string, string> preloaded_lines = new Dictionary<string, string>();

            foreach (string line in lines)
            {               
                if (line.CheckString() && !line.StartsWith("//"))
                {
                    Debug.SubHeader(" Preloading command...");

                    int parse_point = line.IndexOf(" ");

                    if (parse_point != -1)
                    {
                        try
                        {
                            key = line.Substring(0, parse_point);
                            value = line.Substring(parse_point + 1);

                            preloaded_lines.Add(key, value);

                            Debug.Success(DebugMethod.PreLoad, DebugObject.Command, line);
                            Debug.SubText("Key: " + key);
                            Debug.SubText("Value: " + value);

                        }
                        catch (Exception ex)
                        {
                            Debug.Failed(DebugMethod.PreLoad, DebugObject.Command, DebugError.Exception);
                            Debug.SubText("Command: " + line);
                            Debug.SubText("Exception: " + ex.Message);
                        }
                    }
                    else
                    {
                        Debug.Failed(DebugMethod.PreLoad, DebugObject.Command, DebugError.Null);
                        Debug.SubText("Command: "+ line);
                    }
                }
            }

            return preloaded_lines;
        }

        /// <summary>
        /// Loads a command with a given response into the <see cref="commands"/> dictionary on launch.
        /// </summary>
        /// <param name="command">Command key to be added.</param>
        /// <param name="response">What is returned when the command key is called.</param>
        /// <param name="permanent">Determines if the command can be removed.</param>
        /// <param name="variables">Parses the response for any valid variables and loads them into the <see cref="Variables.variables"/> dictionary.</param>
        /// <param name="message">(Optional parameter) Required to send a chat message or whisper by calling <see cref="Notify"/>.Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">(Optional parameter) Required to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        /// <returns></returns>
        private bool Load(string command, string response, bool permanent, Variables variables, Message message = null, TwitchBot bot = null)
        {
            UserType permission;
            CommandType command_type;

            bool send_response = message != null && bot != null;

            Debug.BlankLine();

            response = ParseResponse(response, variables, out command_type, out permission);

            //everyone is a viewer when they send a whisper, no point in specifiying a commmand with a specified permisison value
            if(command_type == CommandType.Whisper && permission != UserType.viewer)
            {
                Debug.Notify($"Changing permission from {permission} to viewer");

                permission = UserType.viewer;
            }

            Debug.SubHeader(" Loading command...");

            //check to see if the syntax is correct
            if (!CheckSyntax(command, response))
            {
                if (send_response)
                {
                    Notify.Failed(bot, DebugMethod.Add, DebugError.Syntax, message, command);
                }

                Debug.Failed(DebugMethod.Load, DebugObject.Command, DebugError.Syntax);
                Debug.SubText("Command: " + command);
                Debug.SubText("Response: " + response);

                return false;
            }

            //check to see if the command already exists
            if (Exists(command))
            {
                if (send_response)
                {
                    Notify.Failed(bot, DebugMethod.Add, DebugError.ExistYes, message, command);
                }

                Debug.Failed(DebugMethod.Load, DebugObject.Command, DebugError.ExistYes);
                Debug.SubText("Command: " + command);

                return false;
            }

            try
            {
                //assuming everything went right, add the command to the dictionary
                commands.Add(command, new Command(permission, command, response, permanent, command_type));

                if (send_response)
                {
                    Notify.Success(bot, DebugMethod.Add, message, command);
                }

                Debug.Success(DebugMethod.Load, DebugObject.Command, command);
                Debug.SubText("Command: " + command);
                Debug.SubText("Response: " + response);
                Debug.SubText("Permanent: " + permanent);
                Debug.SubText("Permission: " + permission);
                Debug.SubText("Command type: " + command_type);
            }
            catch (Exception ex)
            {
                //shit hit the fan, something went wrong
                if (send_response)
                {
                    Notify.Failed(bot, DebugMethod.Add, DebugError.Exception, message, command);
                }

                Debug.Failed(DebugMethod.Load, DebugObject.Command, DebugError.Exception);
                Debug.SubText("Command: " + command);
                Debug.SubText("Exception: " + ex.Message);

                return false;
            }

            return true;
        }

        #endregion

        #region Add, Edit, and Remove commands

        /// <summary>
        /// Parses the body of a <see cref="Message"/> for a command and attempts to add the specified command.
        /// Called from Twitch by using <code>!addcommand</code>.
        /// </summary>
        /// <param name="variables">Parses the response for any valid variables and loads them into the <see cref="Variables.variables"/> dictionary.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">Used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        public void Add(Variables variables, Message message, TwitchBot bot)
        {
            Debug.SubHeader(" Adding command...");

            message.body = variables.ParseLoopAdd(message.body);

            KeyValuePair<string, string> command = ParseCommandKVP(message);

            Add(command.Key, command.Value, variables, message, bot);
        }

        /// <summary>
        /// Adds a command with a given reponse into the <see cref="commands"/> dictionary in real time.
        /// </summary>
        /// <param name="command">Command key to be added.</param>
        /// <param name="response">What is returned when the command key is called.</param>
        /// <param name="variables">Parses the response for any valid variables and loads them into the <see cref="Variables.variables"/> dictionary.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">Used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        private void Add(string command, string response, Variables variables, Message message, TwitchBot bot)
        {
            string text = command + " " + response;

            //only add the command to the text file if it successfully adds the command
            if (Load(command, response, false, variables, message, bot))
            {                
                text.AppendToFile(file_path_normal);
            }
        }

        /// <summary>
        /// Parses the body of a <see cref="Message"/> for a command and attempts to edit the specified command.
        /// Called from Twitch by using <code>!editcommand.</code>
        /// </summary>
        /// <param name="variables">Parses the response for any valid variables and loads them into the <see cref="Variables.variables"/> dictionary.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">Used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        public void Edit(Variables variables, Message message, TwitchBot bot)
        {
            Debug.SubHeader(" Editing command...");                       

            KeyValuePair<string, string> command = ParseCommandKVP(message);

            Edit(command.Key, command.Value, variables, message, bot);
        }

        /// <summary>
        /// Edits the response of a given command in the <see cref="commands"/> dictionary in real time.
        /// </summary>
        /// <param name="command">Command key to be edited.</param>
        /// <param name="response">What is returned when the command key is called.</param>
        /// <param name="variables">Parses the response for any valid variables and loads them into the <see cref="Variables.variables"/> dictionary.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">Used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        private void Edit(string command, string response, Variables variables, Message message, TwitchBot bot)
        {
            //check to see if the syntax is correct
            if (!CheckSyntax(command, response))
            {
                Notify.Failed(bot, DebugMethod.Edit, DebugError.Syntax, message, command);

                Debug.Failed(DebugMethod.Edit, DebugObject.Command, DebugError.Syntax);
                Debug.SubText("Command: " + command);
                Debug.SubText("Response: " + response);                               

                return;
            }

            //make sure the user isn't trying to edit a command that doesn't exists
            if (!Exists(command))
            {
                Notify.Failed(bot, DebugMethod.Edit, DebugError.ExistNo, message, command);

                Debug.Failed(DebugMethod.Edit, DebugObject.Command, DebugError.ExistNo);
                Debug.SubText("Command: " + command);

                return;
            }

            try
            {
                bool permanent = commands[command].permanent;

                string file_path,
                       to_append = command;

                UserType permission;
                CommandType command_type;

                if (permanent)
                {
                    file_path = file_path_permanent;
                }
                else
                {
                    file_path = file_path_normal;
                }

                //only remove the lines where the command key is the first word, aka where the command is defined
                command.RemoveFromFile(file_path, FileFilter.StartsWith);                               
                
                response = ParseResponse(response, variables, out command_type, out permission);

                commands[command] = new Command(permission, command, response, permanent, command_type);

                //only append the command type if it is not the default value
                if(command_type != CommandType.Both)
                {
                    to_append += " [" + command_type.ToString() + "]";
                }

                //only append the permission if it is not the default value
                if (permission != UserType.viewer)
                {
                    to_append += " [" + permission.ToString() + "]";
                }

                to_append += " " + response;

                to_append.AppendToFile(file_path);

                Notify.Success(bot, DebugMethod.Edit, message, command);

                Debug.Success(DebugMethod.Edit, DebugObject.Command, command);
                Debug.SubText("Command: " + command);
                Debug.SubText("Response: " + response);
            }
            catch (Exception ex)
            {
                //shit hit the fan, something went wrong 
                Notify.Failed(bot, DebugMethod.Edit, DebugError.Exception, message, command);

                Debug.Failed(DebugMethod.Edit, DebugObject.Command, DebugError.Exception);
                Debug.SubText("Command: " + command);
                Debug.SubText("Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Parses the body of a <see cref="Message"/> for a command and attempts to remove the specified command.
        /// Called from Twitch by using <code>!removecommand</code>.
        /// </summary>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">Used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        public void Remove(Message message, TwitchBot bot)
        {
            Debug.SubHeader(" Removing command...");

            string command = ParseCommandString(message);

            Remove(command, message, bot);
        }

        /// <summary>
        /// Removed the specified command from the <see cref="commands"/> dictionary in real time.
        /// </summary>
        /// <param name="command">Command key to be removed.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">Used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        private void Remove(string command, Message message, TwitchBot bot)
        {
            //make sure the user isn't trying to remove a command that doesn't exist
            if (!Exists(command))
            {
                Notify.Failed(bot, DebugMethod.Remove, DebugError.ExistNo, message, command);

                Debug.Failed(DebugMethod.Remove, DebugObject.Command, DebugError.ExistNo);
                Debug.SubText("Command: " + command);               

                return;
            }

            //check to see the command can actually be removed
            if (isPermanent(command))
            {
                Notify.Failed(bot, DebugMethod.Remove, DebugError.Permanent, message, command);

                Debug.Failed(DebugMethod.Remove, DebugObject.Command, DebugError.Permanent);
                Debug.SubText("Command: " + command);

                return;
            }

            try
            {
                //remove any line that STARTS with the command
                //just in case there is a "!help" type command that lists other command keys
                command.RemoveFromFile(file_path_normal, FileFilter.StartsWith);

                commands.Remove(command);

                Notify.Success(bot, DebugMethod.Remove, message, command);

                Debug.Success(DebugMethod.Remove, DebugObject.Command, command);
                Debug.SubText("Command: " + command);
            }
            catch (Exception ex)
            {
                //something happened, abort! abort!
                Notify.Failed(bot, DebugMethod.Remove, DebugError.Exception, message, command);

                Debug.Failed(DebugMethod.Remove, DebugObject.Command, DebugError.Exception);
                Debug.SubText("Command: " + command);
                Debug.SubText("Exception: " + ex.Message);
            }

            return;
        }

        #endregion

        #region Extract command information

        /// <summary>
        /// Parses through a string and checks to see if if the string contains a command.
        /// </summary>
        /// <param name="body">The string to be parsed and checked for a command.</param>
        /// <returns></returns>
        public Command ExtractCommand(string body)
        {
            string[] words = body.StringToArray<string>(' ');

            foreach (string word in words)
            {
                if (commands.ContainsKey(word))
                {
                    return commands[word];
                }
            }

            return new Command(UserType.viewer, string.Empty, string.Empty, false, CommandType.Both);
        }

        /// <summary>
        /// Gets the response for the specified command key. 
        /// Replaces any valid variables in the response with their appropriate values.
        /// </summary>
        /// <param name="command">Command key to be get the response from.</param>
        /// <param name="variables">Parses the response for any valid variables and replaces them with their appropriate value.</param>
        /// <returns></returns>
        public string GetResponse(string command, Variables variables)
        {
            if (commands.ContainsKey(command))
            {
                string response;

                response = commands[command].response;

                //search to see if the command response has a variable and replace it with its value
                foreach (KeyValuePair<string, string> pair in variables.GetVariables())
                {
                    if (response.Contains(pair.Key))
                    {
                        response = response.Replace(pair.Key, pair.Value);
                    }
                }

                return response;
            }

            return string.Empty;
        }       

        /// <summary>
        /// Checks to see if the permission matches the proper syntax.
        /// </summary>
        /// <param name="permission">Permission value to check.</param>
        /// <returns></returns>
        private bool CheckSeparateSyntax(string permission)
        {
            if(permission.StartsWith("[") && permission.EndsWith("]"))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Bollean checks

        /// <summary>
        /// Checks to see if a command already exists.
        /// </summary>
        /// <param name="command">Command key to check.</param>
        /// <returns></returns>
        public bool Exists(string command)
        {
            return commands.ContainsKey(command);
        }

        /// <summary>
        /// Checks to see if a command can be removed.
        /// </summary>
        /// <param name="command">Command key to check.</param>
        /// <returns></returns>
        public bool isPermanent(string command)
        {
            return commands[command].permanent;
        }

        /// <summary>
        /// Checks to see if the command and response match the proper syntax.
        /// </summary>
        /// <param name="command">Command key to be checked.</param>
        /// <param name="response">Response to be checked.</param>
        /// <returns></returns>
        private bool CheckSyntax(string command, string response)
        {
            if (!command.CheckString())
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Command, DebugObject.Command, SyntaxError.Null);
                Debug.SubText("Command: null");

                return false;
            }

            if (!response.CheckString())
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Command, DebugObject.Response, SyntaxError.Null);
                Debug.SubText("Command: " + command);
                Debug.SubText("Response: null");

                return false;
            }

            if (!command.StartsWith("!"))
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Command, DebugObject.Command, SyntaxError.EexclamationPoint);
                Debug.SubText("Command: " + command);

                return false;
            }

            //command needs at least one character after "!"
            if(command.Length < 2)
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Command, DebugObject.Command, SyntaxError.Length);
                Debug.SubText("Command length: " + command.Length);
                Debug.SubText("Minimum length: 2");

                return false;
            }

            return true;
        }

        #endregion

        #region Command wrappers

        //  ----------------------------------------------------------------  \\
        //  basically things that can't be done natively with the twitch api  \\
        //  ----------------------------------------------------------------  \\

        /// <summary>
        /// Gets the uptime of the specified broadcaster by assembling the uptime <see cref="string"/> fragments.
        /// Called from Twitch by using <code>!uptime</code>.
        /// </summary>
        /// <param name="broadcaster">Contains the method to get the uptime in <see cref="TimeSpan"/> format. Also contains the broadcaster information.</param>
        /// <returns></returns>
        public string GetUpTime(TwitchUserAuthenticated broadcaster)
        {
            if (!broadcaster.isLive(broadcaster.name))
            {
                return $"{broadcaster.display_name} is currently offline";
            }

            string total_time, prefix;

            TimeSpan time = broadcaster.GetUpTime(broadcaster.display_name);

            string hours = GetTimeString(time.Hours, "hour"),
                   minutes = GetTimeString(time.Minutes, "minute"),
                   seconds = GetTimeString(time.Seconds, "second");

            prefix = broadcaster.display_name + " has been streaming for ";

            //hours does not have a value
            if (!hours.CheckString())
            {
                //(0, 0, 0)
                if (!minutes.CheckString() && !seconds.CheckString())
                {
                    total_time = "currently offline";
                }
                //(0, 0, 1)
                else if (!minutes.CheckString() && seconds.CheckString())
                {
                    total_time = seconds;
                }
                //(0, 1, 0)
                else if (minutes.CheckString() && !seconds.CheckString())
                {
                    total_time = minutes;
                }
                //(0, 1, 1)
                else
                {
                    total_time = minutes + " and " + seconds;
                }
            }
            //hours has a value
            else
            {
                //(1, 0, 0)
                if (!minutes.CheckString() && !seconds.CheckString())
                {
                    total_time = hours;
                }
                //(1, 0, 1)
                else if (!minutes.CheckString() && seconds.CheckString())
                {
                    total_time = hours + " and " + seconds;
                }
                //(1, 1, 0)
                else if (minutes.CheckString() && !seconds.CheckString())
                {
                    total_time = hours + " and " + minutes;
                }
                //(1, 1, 1)
                else
                {
                    total_time = hours + ", " + minutes + ", and " + seconds;
                }
            }

            return prefix + total_time;
        }

        /// <summary>
        /// Converts the uptime fragment from <see cref="TimeSpan"/> to a displayable <see cref="string"/>.
        /// </summary>
        /// <param name="value">Nullable uptime fragment in <see cref="TimeSpan"/> format.</param>
        /// <param name="tier">Uptime tier: hour, minute, or second.</param>
        /// <returns></returns>
        private string GetTimeString(int? value, string tier)
        {
            string to_return = $"{value.ToString()} {tier}";

            if (value == 0 || value == null)
            {
                return string.Empty;
            }
            else if (value == 1)
            {
                return to_return;
            }
            else
            {
                return to_return + "s";
            }
        }

        /// <summary>
        /// Updates the broadcaster's game, title, or stream delay.
        /// Note: The broadcaster must be a partner to set a delay.
        /// Requires the "channel_editor" scope.
        /// </summary>
        /// <param name="settings">Stream setting to update.</param>
        /// <param name="message">The message to be parsed for the new stream setting.</param>
        /// <param name="broadcaster">Contains the method to update the stream setting. Also contains the broadcaster information.</param>
        public void UpdateStream(StreamSetting settings, Message message, TwitchUserAuthenticated broadcaster)
        {
            string setting = settings.ToString(),
                   value = ParseCommandString(message);

            string _value = "";

            if (!value.CheckString())
            {
                Debug.Failed($"Failed to set {setting}: title cannot be null or empty");

                return;
            }

            if(settings == StreamSetting.Delay)
            {
                //the value wasn't a number
                if (!value.CanCovertTo(typeof(double)))
                {
                    Debug.Failed($"Could not convert {value} to {typeof(double).FullName}");

                    return;
                }

                //the channel isn't partnered 
                if (!broadcaster.isPartner(broadcaster.name))
                {
                    Debug.Failed($"Failed to set delay: you need to be partnered to have this option");

                    return;
                }
            }

            //there's no channel specified
            if (!broadcaster.display_name.CheckString())
            {
                Debug.Failed($"Failed to set {setting}: no channel name specified");

                return;
            }

            switch (settings)
            {
                case StreamSetting.Delay:
                    broadcaster.SetDelay(broadcaster.display_name.ToLower(), value);

                    _value = broadcaster.GetChannel(broadcaster.display_name).delay.ToString();
                    break;
                case StreamSetting.Game:
                    broadcaster.SetGame(broadcaster.display_name.ToLower(), value);

                    _value = broadcaster.GetChannel(broadcaster.display_name).game;
                    break;
                case StreamSetting.Title:
                    broadcaster.SetTitle(broadcaster.display_name.ToLower(), value);

                    _value = broadcaster.GetChannel(broadcaster.display_name).status;
                    break;
                default:
                    break;
            }

            //this works but it takes time for the server to update it so it appears to not have been changed when offline
            Debug.Success($"Successfully updated {setting}!");
            Debug.SubText(setting + ": " + _value);
            Debug.SubText("Channel: " + broadcaster.display_name);
        }

        /// <summary>
        /// Gets the name of the current song playing.
        /// Reads the name fromt he file path specified from the <code>!music</code> response.
        /// </summary>
        /// <returns></returns>
        public string GetCurrentSong()
        {
            try
            {
                string song = File.ReadAllText(commands["!music"].response);

                if (song.CheckString())
                {
                    return "Current song: " + song;
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                Debug.Failed("Failed to get current song: unkown error");
                Debug.SubText("Exception: " + ex.Message);

                return "Failed to retrieve song data";
            }

        }

        /// <summary>
        /// Gets how long a channel has been following the broadcaster.
        /// </summary>
        /// <param name="channel">The name of the broadcaster.</param>
        /// <param name="user">The name of the user to check.</param>
        /// <returns></returns>
        public string GetHowLong(string channel, string user)
        {
            string web_text = GetWebText("http://api.newtimenow.com/follow-length/?channel=" + channel + "&user=" + user),
                   how_long = $"{user} is not following {channel}";

            if (channel.ToLower() == user.ToLower())
            {
                return $"You cannot follow yourself {user} FailFish";
            }

            if (web_text.Contains("Not following"))
            {
                return how_long;
            }

            try
            {
                DateTime date_followed = DateTime.Parse(web_text).ToLocalTime();

                how_long = $"{user} has been following {channel} since {date_followed.ToShortDateString()} PogChamp";
            }
            catch (Exception ex)
            {
                how_long = $"Could not retrieve follow time at this time {user} BibleThump";

                Debug.Failed($"Failed to get how long {user} has been following {channel}");
                Debug.SubText("Exception: " + ex.Message);
            }

            return how_long;
        }

        /// <summary>
        /// Downloads the text from a web page.
        /// </summary>
        /// <param name="url">URL of the web page.</param>
        /// <returns></returns>
        public string GetWebText(string url)
        {
            WebClient web_client = new WebClient();

            return web_client.DownloadString(url);
        }

        #endregion

        #region String parsing

        /// <summary>
        /// Searches the response of a <see cref="Command"/> for the command type or the permission level.
        /// </summary>
        /// <typeparam name="TEnum">A generic <see cref="Enum"/> that can either be <see cref="CommandType"/> or <see cref="UserType"/>. Specifies which command parameter to search for.</typeparam>
        /// <param name="response">What is returned when the command key is called.</param>
        /// <param name="default_value">The default value of the <see cref="CommandType"/> or <see cref="UserType"/> to return if no value is specified in the response</param>
        /// <returns></returns>
        private KeyValuePair<TEnum, string> SeparateResponse<TEnum>(string response, TEnum default_value) where TEnum : struct 
        {
            string separate_tag, 
                   value = "";

            string[] response_array = response.StringToArray<string>(' ');

            TEnum enum_value = default_value;

            if(enum_value.GetType() == typeof(UserType))
            {
                separate_tag = "Permission";
            }
            else
            {
                separate_tag = "Command type";
            }

            Debug.SubHeader($" Separating response for {separate_tag.ToLower()} ...");

            //the response is empty, return an empty string  
            if (!response.CheckString())
            {
                Debug.Failed(DebugMethod.Separate, DebugObject.Command, DebugError.Null);
                Debug.SubText("Response: null");

                return new KeyValuePair<TEnum, string>(enum_value, value);
            }

            //nothing after !addcommand
            if (!response_array.CheckArray())
            {
                Debug.Failed(DebugMethod.Separate, DebugObject.Command, DebugError.Null);

                return new KeyValuePair<TEnum, string>(enum_value, value);
            }

            //check to see if the first word is a valid permission level and has the right syntax
            if (response_array[0].Length > 1 && Enum.TryParse(response.Substring(1, response_array[0].Length - 2), out enum_value) && CheckSeparateSyntax(response_array[0]))
            {
                if (response_array[0].Length < response.Length)
                {
                    //valid permisison and there was a response after it
                    value = response.Substring(response_array[0].Length + 1);

                    Debug.Success(DebugMethod.Separate, DebugObject.Command, $"{response} ({ separate_tag.ToLower()} specified)");
                    Debug.SubText(separate_tag + ": " + enum_value);
                    Debug.SubText("Response: " + value);
                }
                else
                {
                    //there was nothing after the permission
                    Debug.Failed(DebugMethod.Separate, DebugObject.Command, DebugError.Null);
                    Debug.SubText(separate_tag + ": " + enum_value);
                    Debug.SubText("Response: null");
                }
            }
            else
            {
                //the first word wasn't a permision value, set the response to the entire string
                value = response;

                Debug.Success(DebugMethod.Separate, DebugObject.Command, $"{response} (no { separate_tag.ToLower()} specified)");
                Debug.SubText(separate_tag + ": " + enum_value);
                Debug.SubText("Response: " + value);
            }

            return new KeyValuePair<TEnum, string>(enum_value, value);
        }


        /// <summary>
        /// Parses the body of a <see cref="Message"/> after the command and returns a <see cref="KeyValuePair{TKey, TValue}"/>.
        /// The first word after the command is the <code>Key</code> and evertything after the <code>TKey</code> is the <code>TValue</code>.
        /// </summary>
        /// <param name="message">Contains the body and command of the message that is parsed.</param>
        /// <returns></returns>
        public KeyValuePair<string, string> ParseCommandKVP(Message message)
        {
            int parse_start, parse_end;

            string key = "",
                   value = "";

            //parse the message for the command key
            parse_start = message.body.IndexOf(message.command.key) + message.command.key.Length + 1;
            parse_end = message.body.Substring(parse_start).IndexOf(" ") + parse_start;

            try
            {
                key = message.body.Substring(parse_start, parse_end - parse_start);
                value = message.body.Substring(parse_end + 1);

                Debug.Success(DebugMethod.ParseKVP, DebugObject.Command, message.body);
                Debug.SubText("Key: " + key);
                Debug.SubText("Value: " + value);
            }
            catch (Exception ex)
            {
                Debug.Failed(DebugMethod.ParseKVP, DebugObject.Command, DebugError.Exception);
                Debug.SubText("Line: " + message.body);
                Debug.SubText("Exception: " + ex.Message);
            }

            return new KeyValuePair<string, string>(key, value);
        }

        /// <summary>
        /// Parses the body of a <see cref="Message"/> after the command and returns a <see cref="string"/>.
        /// </summary>
        /// <param name="message">Contains the body and command of the message that is parsed.</param>
        /// <returns></returns>
        public string ParseCommandString(Message message)
        {
            int parse_start = message.body.IndexOf(message.command.key) + message.command.key.Length;

            string result = "";

            try
            {
                result = message.body.Substring(parse_start + 1);

                Debug.Success(DebugMethod.ParseString, DebugObject.Command, message.body);
                Debug.SubText("String: " + result);
            }
            catch (Exception ex)
            {
                Debug.Failed(DebugMethod.ParseString, DebugObject.Command, DebugError.Exception);
                Debug.SubText("Line: " + message.body);
                Debug.SubText("Exception: " + ex.Message);
            }

            return result;
        }

        private string ParseResponse(string response, Variables variables, out CommandType command_type, out UserType permission)
        {
            //check where the command can be used
            KeyValuePair<CommandType, string> command_type_response = SeparateResponse(response, CommandType.Both);
            command_type = command_type_response.Key;
            response = command_type_response.Value;

            //get who can use the command
            KeyValuePair<UserType, string> permisison_response = SeparateResponse(response, UserType.viewer);
            permission = permisison_response.Key;
            response = permisison_response.Value;

            //now the command type and permisison have been parsed, search for any variables in the actual response
            return variables.ParseLoopAdd(response);
        }

        #endregion
    }
}