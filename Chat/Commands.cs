using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using Newtonsoft.Json;

using TwitchChatBot.Clients;
using TwitchChatBot.Debugger;
using TwitchChatBot.Enums.Chat;
using TwitchChatBot.Enums.Debug;
using TwitchChatBot.Extensions;
using TwitchChatBot.Extensions.Files;
using TwitchChatBot.Models.Bot;

namespace TwitchChatBot.Chat
{
    class Commands
    {
        string file_path = Environment.CurrentDirectory + "/JSON/Chat/Commands.json";

        List<Command> commands_list = new List<Command>();

        Dictionary<string, Command> commands_dictionary = new Dictionary<string, Command>();       

        public Commands(Variables variables)
        {
            string commands_preloaded;

            List<Command> commands_preloaded_list;

            Debug.BlankLine();

            Debug.BlockBegin();
            Debug.Header("Loading Commands");
            Debug.PrintLine("File path:", file_path);

            commands_preloaded = File.ReadAllText(file_path);
            commands_preloaded_list = JsonConvert.DeserializeObject<List<Command>>(commands_preloaded);

            if (commands_preloaded_list != null)
            {
                foreach (Command command in commands_preloaded_list)
                {
                    Load(command);
                }
            }

            Debug.BlockEnd();
        }

        #region Load commands

        /// <summary>
        /// Loads a <see cref="Command"/> into the <see cref="commands_list"/> and the <see cref="commands_dictionary"/> to be used in real time.
        /// </summary>
        /// <param name="command">The command to load.</param>
        private void Load(Command command)
        {
            Debug.BlankLine();

            Debug.SubHeader("Loading command...");

            if (!CheckSyntax(command))
            {
                //REPLACE .GETNAME() WITH NAMEOF()
                Debug.Error(DebugMethod.Load, DebugObject.Command, DebugError.Syntax);
                Debug.PrintLine(nameof(command.key), command.key);
                Debug.PrintLine(nameof(command.response), command.response);

                return;
            }

            if (Exists(command.key))
            {
                Debug.Error(DebugMethod.Load, DebugObject.Command, DebugError.ExistYes);
                Debug.PrintLine(nameof(command.key), command.key);

                return;
            }

            if (!command.permission.CheckEnumRange<UserType>())
            {
                Debug.SyntaxError(DebugObject.Command, DebugObject.Setting, SyntaxError.EnumRange, Enum.GetNames(typeof(UserType)).Length);
                Debug.PrintLine(nameof(command.permission), ((int)command.permission).ToString());
                Debug.PrintLine(nameof(command.permission) + " set to " + UserType.viewer.ToString());

                command.permission = UserType.viewer;
            }            

            try
            {
                command.last_used = DateTime.MinValue;
                
                commands_list.Add(command);

                commands_dictionary.Add(command.key, command);

                Debug.Success(DebugMethod.Load, DebugObject.Command, command.key);
                Debug.PrintObject(command);
            }
            catch (Exception exception)
            {
                Debug.Error(DebugMethod.Load, DebugObject.Command, DebugError.Exception);
                Debug.PrintLine(nameof(command.key), command.key);
                Debug.PrintLine(nameof(exception), exception.Message);

                return;
            }
        }

        #endregion

        #region Add, Edit, and Remove commands

        /// <summary>
        /// Parses the body of a <see cref="Message"/> for a command and attempts to add the extracted command.
        /// Called from Twitch by using <code>!addcommand</code>.
        /// </summary>
        /// <param name="variables">Parses the response for any valid variables and loads them into the <see cref="Variables.variables_dictionary"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        public void Add(Variables variables, Message message)
        {
            Debug.BlankLine();
            Debug.SubHeader("Adding command...");           

            Command command = MessageToCommand(DebugMethod.Add, message, variables);

            if (command == null)
            {
                return;
            }

            Add(command, variables, message);
        }

        /// <summary>
        /// Adds a command with a given reponse into the <see cref="commands_dictionary"/> in real time.
        /// </summary>
        /// <param name="command">Command to be added.</param>
        /// <param name="variables">Parses the response for any valid variables and loads them into the <see cref="Variables.variables_dictionary"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        private void Add(Command command, Variables variables, Message message)
        {           
            if (!CheckSyntax(command))
            {
                Notify.Failed(DebugMethod.Add, DebugObject.Command, command.key, DebugError.Syntax, message);

                Debug.Error(DebugMethod.Add, DebugObject.Command, DebugError.Syntax);
                Debug.PrintLine(nameof(command.key), command.key);
                Debug.PrintLine(nameof(command.response), command.response);

                return;
            }

            if (Exists(command.key))
            {
                Notify.Failed(DebugMethod.Add, DebugObject.Command, command.key, DebugError.ExistYes, message);

                Debug.Error(DebugMethod.Add, DebugObject.Command, DebugError.ExistYes);
                Debug.PrintLine(nameof(command.key), command.key);

                return;
            }

            if (!command.permission.CheckEnumRange<UserType>())
            {
                int enum_size = Enum.GetNames(typeof(UserType)).Length;

                Debug.SyntaxError(DebugObject.Command, DebugObject.Setting, SyntaxError.EnumRange, Enum.GetNames(typeof(UserType)).Length);
                Debug.PrintLine(nameof(command.permission), ((int)command.permission).ToString());
                Debug.PrintLine(nameof(command.permission) + "set to " + UserType.viewer.ToString());

                command.permission = UserType.viewer;
            }

            try
            {
                commands_list.Add(command);

                JsonConvert.SerializeObject(commands_list, Formatting.Indented).OverrideFile(file_path);
           
                commands_dictionary.Add(command.key, command);

                Notify.Success(DebugMethod.Add, DebugObject.Command, command.key, message);

                Debug.Success(DebugMethod.Add, DebugObject.Command, command.key);
                Debug.PrintObject(command);
            }
            catch (Exception exception)
            {
                Notify.Failed(DebugMethod.Add, DebugObject.Command, command.key, DebugError.Exception, message);

                Debug.Error(DebugMethod.Add, DebugObject.Command, DebugError.Exception);
                Debug.PrintLine(nameof(command.key), command.key);
                Debug.PrintLine(nameof(exception), exception.Message);

                return;
            }
        }

        /// <summary>
        /// Parses the body of a <see cref="Message"/> for a command and attempts to edit the specified command.
        /// Called from Twitch by using <code>!editcommand.</code>
        /// </summary>
        /// <param name="variables">Parses the response for any valid variables and loads them into the <see cref="Variables.variables_dictionary"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        public void Edit(Variables variables, Message message)
        {
            Debug.BlankLine();
            Debug.SubHeader("Editing command...");

            Command command = MessageToCommand(DebugMethod.Edit, message, variables);

            if (command == null)
            {
                return;
            }

            Edit(command, variables, message);
        }

        /// <summary>
        /// Edits the response of a given command in the <see cref="commands_dictionary"/> in real time.
        /// </summary>
        /// <param name="command_model">Command to be edited.</param>
        /// <param name="variables">Parses the response for any valid variables and loads them into the <see cref="Variables.variables_dictionary"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        private void Edit(Command command_model, Variables variables, Message message)
        {
            string preserialized_command = ParseCommandString(message).PreserializeAs<string>();        
                        
            if (!Exists(command_model.key))
            {
                Notify.Failed(DebugMethod.Edit, DebugObject.Command, command_model.key, DebugError.ExistNo, message);

                Debug.Error(DebugMethod.Edit, DebugObject.Command, DebugError.ExistNo);
                Debug.PrintLine(nameof(command_model.key), command_model.key);

                return;
            }

            Command command = new Command
            {
                key = command_model.key,
                response = preserialized_command.Contains("\"response\":") ? command_model.response : commands_dictionary[command_model.key].response,
                permanent = preserialized_command.Contains("\"permanent\":") ? command_model.permanent : commands_dictionary[command_model.key].permanent,
                permission = preserialized_command.Contains("\"permission\":") ? command_model.permission : commands_dictionary[command_model.key].permission,
                type = preserialized_command.Contains("\"type\":") ? command_model.type : commands_dictionary[command_model.key].type,
                cooldown = preserialized_command.Contains("\"cooldown\":") ? command_model.cooldown : commands_dictionary[command_model.key].cooldown
            };

            if (!CheckSyntax(command))
            {
                Notify.Failed(DebugMethod.Edit, DebugObject.Command, command.key, DebugError.Syntax, message);

                Debug.Error(DebugMethod.Edit, DebugObject.Command, DebugError.Syntax);
                Debug.PrintLine(nameof(command.key), command.key);
                Debug.PrintLine(nameof(command.response), command.response);                               

                return;
            }

            if (!command.permission.CheckEnumRange<UserType>())
            {
                int enum_size = Enum.GetNames(typeof(UserType)).Length - 1;

                Debug.SyntaxError(DebugObject.Command, DebugObject.Setting, SyntaxError.EnumRange, Enum.GetNames(typeof(UserType)).Length);
                Debug.PrintLine(nameof(command.permission), ((int)command.permission).ToString());
                Debug.PrintLine(nameof(command.permission) + "set to " + commands_dictionary[command_model.key].permission.ToString());

                command.permission = commands_dictionary[command_model.key].permission;
            }

            try
            {
                commands_list.Remove(commands_dictionary[command.key]);
                commands_list.Add(command);

                commands_dictionary[command.key] = command;

                JsonConvert.SerializeObject(commands_list, Formatting.Indented).OverrideFile(file_path);

                Notify.Success(DebugMethod.Edit, DebugObject.Command, command.key, message);

                Debug.Success(DebugMethod.Edit, DebugObject.Command, command.key);
                Debug.PrintLine(nameof(command.key), command.key);
                Debug.PrintLine(nameof(command.response), command.response);
            }
            catch (Exception exception)
            {
                Notify.Failed(DebugMethod.Edit, DebugObject.Command, command.key, DebugError.Exception, message);

                Debug.Error(DebugMethod.Edit, DebugObject.Command, DebugError.Exception);
                Debug.PrintLine(nameof(command.key), command.key);
                Debug.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Parses the body of a <see cref="Message"/> for a command and attempts to remove the specified command.
        /// Called from Twitch by using <code>!removecommand</code>.
        /// </summary>
        /// <param name="variables">Required to create a command model in order for the command to be removed properly.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        public void Remove(Variables variables, Message message)
        {
            Debug.BlankLine();
            Debug.SubHeader("Removing command...");

            Command command = MessageToCommand(DebugMethod.Remove, message, variables);

            if (command == null)
            {
                return;
            }

            Remove(command, variables, message);
        }

        /// <summary>
        /// Removed the specified command from the <see cref="commands_dictionary"/> dictionary in real time.
        /// </summary>
        /// <param name="command">Command to be removed.</param>
        /// <param name="variables">Required to create a command model in order for the command to be removed properly.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        private void Remove(Command command, Variables variables, Message message)
        {
            if (!Exists(command.key))
            {
                Notify.Failed(DebugMethod.Remove, DebugObject.Command, command.key, DebugError.ExistNo, message);

                Debug.Error(DebugMethod.Remove, DebugObject.Command, DebugError.ExistNo);
                Debug.PrintLine(nameof(command.key), command.key);               

                return;
            }

            if (isPermanent(command.key))
            {
                Notify.Failed(DebugMethod.Remove, DebugObject.Command, command.key, DebugError.Permanent, message);

                Debug.Error(DebugMethod.Remove, DebugObject.Command, DebugError.Permanent);
                Debug.PrintLine(nameof(command.key), command.key);

                return;
            }

            try
            {
                commands_list.Remove(commands_dictionary[command.key]);
                commands_dictionary.Remove(command.key);                

                JsonConvert.SerializeObject(commands_list, Formatting.Indented).OverrideFile(file_path);

                Notify.Success(DebugMethod.Remove, DebugObject.Command, command.key, message);

                Debug.Success(DebugMethod.Remove, DebugObject.Command, command.key);
                Debug.PrintLine(nameof(command.key), command.key);
            }
            catch (Exception exception)
            {
                Notify.Failed(DebugMethod.Remove, DebugObject.Command, command.key, DebugError.Exception, message);

                Debug.Error(DebugMethod.Remove, DebugObject.Command, DebugError.Exception);
                Debug.PrintLine(nameof(command.key), command.key);
                Debug.PrintLine(nameof(exception), exception.Message);
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
        public Command ExtractCommand(string message_body)
        {
            string[] words = message_body.StringToArray<string>(' ');

            foreach (string word in words)
            {
                if (commands_dictionary.ContainsKey(word))
                {
                    return commands_dictionary[word];
                }
            }

            return new Command
            {
                key = string.Empty,
                response = string.Empty,
                permanent = false,
                permission = UserType.viewer,
                type = CommandType.Both,
                last_used = DateTime.MinValue,

            };
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
            if (commands_dictionary.ContainsKey(command))
            {
                string response;

                response = commands_dictionary[command].response;

                //search to see if the command response has a variable and replace it with its value
                foreach (KeyValuePair<string, Variable> pair in variables.GetVariables())
                {
                    if (response.Contains(pair.Key))
                    {
                        response = response.Replace(pair.Key, pair.Value.value);
                    }
                }

                return response;
            }

            return string.Empty;
        }       

        #endregion

        #region Boolean checks

        

        /// <summary>
        /// Checks to see if a command already exists.
        /// </summary>
        /// <param name="command">Command key to check.</param>
        /// <returns></returns>
        public bool Exists(string command)
        {
            return commands_dictionary.ContainsKey(command);
        }

        /// <summary>
        /// Checks to see if a command can be removed.
        /// </summary>
        /// <param name="command">Command key to check.</param>
        /// <returns></returns>
        public bool isPermanent(string command)
        {
            return commands_dictionary[command].permanent;
        }

        /// <summary>
        /// Checks to see if the command and response match the proper syntax.
        /// </summary>
        /// <param name="command">Command key to be checked.</param>
        /// <param name="response">Response to be checked.</param>
        /// <returns></returns>
        private bool CheckSyntax(Command command)
        {
            if (!command.key.CheckString())
            {
                Debug.SyntaxError(DebugObject.Command, DebugObject.Command, SyntaxError.Null);
                Debug.PrintLine(nameof(command.key), "null");

                return false;
            }

            if (!command.response.CheckString())
            {
                Debug.SyntaxError(DebugObject.Command, DebugObject.Response, SyntaxError.Null);
                Debug.PrintLine(nameof(command.key), command.key);
                Debug.PrintLine(nameof(command.response), "null");

                return false;
            }

            if (!command.key.StartsWith("!"))
            {
                Debug.SyntaxError(DebugObject.Command, DebugObject.Command, SyntaxError.EexclamationPoint);
                Debug.PrintLine(nameof(command.key), command.key);

                return false;
            }


            //command needs at least one character after "!"
            if (command.key.Length < 2)
            {
                Debug.SyntaxError(DebugObject.Command, DebugObject.Command, SyntaxError.Length);
                Debug.PrintLine(nameof(command.key), command.key);
                Debug.PrintLine("length", command.key.Length.ToString());
                Debug.PrintLine("minimum length:", "2");

                return false;
            }

            //check for illegal characters
            if (command.key.Contains("{") || command.key.Contains("}"))
            {
                Debug.SyntaxError(DebugObject.Command, DebugObject.Command, SyntaxError.BracketsNo);
                Debug.PrintLine(nameof(command.key), command.key);

                return false;
            }

            if (command.key.Contains("[") || command.key.Contains("]"))
            {
                Debug.SyntaxError(DebugObject.Command, DebugObject.Command, SyntaxError.SquareBracketsNo);
                Debug.PrintLine(nameof(command.key), command.key);

                return false;
            }

            if (command.key.Contains("(") || command.key.Contains(")"))
            {
                Debug.SyntaxError(DebugObject.Command, DebugObject.Command, SyntaxError.ParenthesisNo);
                Debug.PrintLine(nameof(command.key), command.key);

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
        /// Resets the last time a command was used to the current time.
        /// </summary>
        /// <param name="command">The command to reset the time last used for.</param>
        public void ResetLastUsed(Command command)
        {
            if (!Exists(command.key))
            {
                Debug.Error(DebugMethod.Edit, DebugObject.Command, DebugError.ExistNo);
                Debug.PrintLine("Failed to set last time used");
            }

            commands_dictionary[command.key].last_used = DateTime.Now;
        }

        /// <summary>
        /// Gets the uptime of the specified broadcaster by assembling the uptime <see cref="string"/> fragments.
        /// Called from Twitch by using <code>!uptime</code>.
        /// </summary>
        /// <param name="broadcaster">Contains the method to get the uptime in <see cref="TimeSpan"/> format. Also contains the broadcaster information.</param>
        /// <returns></returns>
        public string GetUpTime(TwitchClientOAuth broadcaster)
        {
            if (!broadcaster.isLive(broadcaster.name))
            {
                return broadcaster.display_name + " is currently offline";
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
            string to_return = value.ToString() + " " + tier;

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
        public void UpdateStream(StreamSetting settings, Message message, TwitchClientOAuth broadcaster)
        {
            string setting = settings.ToString(),
                   value = ParseCommandString(message);

            DebugObject debug_setting;

            Enum.TryParse(setting, out debug_setting);

            if (!value.CheckString())
            {
                Debug.Error(DebugMethod.Update, debug_setting, DebugError.Null);
                Debug.PrintLine("stream setting", setting.ToString());

                return;
            }

            if(settings == StreamSetting.Delay)
            {
                if (!value.CanCovertTo(typeof(double)))
                {
                    Debug.Error(DebugMethod.Update, debug_setting, DebugError.Convert);
                    Debug.PrintLine(nameof(value), value);
                    Debug.PrintLine(nameof(value) + " type", value.GetType().Name.ToLower());
                    Debug.PrintLine("supported type", typeof(double).Name);

                    return;
                }

                if (!broadcaster.isPartner(broadcaster.name))
                {
                    Debug.Error("Failed to set delay: you need to be partnered to have this option");

                    return;
                }
            }

            if (!broadcaster.display_name.CheckString())
            {
                Debug.Error(DebugMethod.Update, debug_setting, DebugError.Null);
                Debug.PrintLine(nameof(broadcaster), "null");

                return;
            }

            switch (settings)
            {
                case StreamSetting.Delay:
                    broadcaster.SetDelay(broadcaster.display_name.ToLower(), value);

                    value = broadcaster.GetChannel(broadcaster.display_name).delay.ToString();
                    break;
                case StreamSetting.Game:
                    broadcaster.SetGame(broadcaster.display_name.ToLower(), value);

                    value = broadcaster.GetChannel(broadcaster.display_name).game;
                    break;
                case StreamSetting.Title:
                    broadcaster.SetTitle(broadcaster.display_name.ToLower(), value);

                    value = broadcaster.GetChannel(broadcaster.display_name).status;
                    break;
                default:
                    break;
            }

            //this works but it takes time for the server to update it so it appears to not have been changed when offline
            Debug.Success(DebugMethod.Update, debug_setting, setting);
            Debug.PrintLine(nameof(broadcaster), broadcaster.display_name);
            Debug.PrintLine(nameof(value), value);
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
                string song = File.ReadAllText(commands_dictionary["!music"].response);

                if (song.CheckString())
                {
                    return "Current song: " + song;
                }
                else
                {
                    return "";
                }
            }
            catch (Exception exception)
            {
                Debug.Error("Failed to get current song: unkown error");
                Debug.PrintLine(nameof(exception), exception.Message);

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
                return "You cannot follow yourself " + user + " FailFish";
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
            catch (Exception exception)
            {
                how_long = "Could not retrieve follow time at this time " + user + " BibleThump";

                Debug.Error("Failed to get how long " + user + " has been following " + channel);
                Debug.PrintLine(nameof(exception), exception.Message);
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
        /// Converts a message recieved from Twitch into a <see cref="Command"/> and returns the command.
        /// Returns null if the message could not be converted to a <see cref="Command"/>.
        /// </summary>
        /// <param name="debug_method">The type of operation being performed.</param>
        /// <param name="variables">Parses the response for any valid variables and loads them into the <see cref="Variables.variables_dictionary"/> dictionary.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <returns></returns>
        private Command MessageToCommand(DebugMethod debug_method, Message message, Variables variables)
        {
            string command_string;

            Variable[] variable_array;

            command_string = ParseCommandString(message);
            command_string = variables.ExtractVariables(command_string, message, out variable_array);
            command_string = command_string.PreserializeAs<Command>();     

            try
            {
                Command command = JsonConvert.DeserializeObject<Command>(command_string);

                Debug.Success(DebugMethod.Serialize, DebugObject.Command, command.key);
                Debug.PrintObject(command);

                foreach (Variable variable in variable_array)
                {
                    variables.Add(variable, message);
                }

                return command; 
            }
            catch (Exception exception)
            {
                Notify.Failed(debug_method, DebugObject.Command, command_string, DebugError.Exception, message);

                Debug.Error(DebugMethod.Serialize, DebugObject.Command, DebugError.Exception);
                Debug.Error(debug_method, DebugObject.Command, DebugError.Null);
                Debug.PrintLine(nameof(command_string), command_string);
                Debug.PrintLine(nameof(exception), exception.Message);

                return null;
            }
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
                Debug.PrintLine(nameof(key), key);
                Debug.PrintLine(nameof(value), value);
            }
            catch (Exception exception)
            {
                Debug.Error(DebugMethod.ParseKVP, DebugObject.Command, DebugError.Exception);
                Debug.PrintLine(nameof(message.body), message.body);
                Debug.PrintLine(nameof(exception), exception.Message);
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
                Debug.PrintLine(nameof(result), result);
            }
            catch (Exception exception)
            {
                Debug.Error(DebugMethod.ParseString, DebugObject.Command, DebugError.Exception);
                Debug.PrintLine(nameof(message.body), message.body);
                Debug.PrintLine(nameof(exception), exception.Message);
            }

            return result;
        }

        #endregion
    }
}