using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using Newtonsoft.Json;

using TwitchChatBot.Debugger;
using TwitchChatBot.Enums.Chat;
using TwitchChatBot.Enums.Debug;
using TwitchChatBot.Enums.Extensions;
using TwitchChatBot.Extensions;
using TwitchChatBot.Extensions.Files;
using TwitchChatBot.Models.Bot;

namespace TwitchChatBot.Chat
{
    class Commands
    {
        readonly string FILE_PATH = Environment.CurrentDirectory + "/JSON/Chat/Commands.json";

        List<Command> commands_list;

        Dictionary<string, Command> commands_dictionary;

        public Commands(Variables variables)
        {
            string commands_preloaded_string;

            List<Command> commands_preloaded_list;

            commands_list = new List<Command>();
            commands_dictionary = new Dictionary<string, Command>();            

            BotDebug.BlankLine();

            BotDebug.BlockBegin();
            BotDebug.Header("Loading Commands");
            BotDebug.PrintLine("File path:", FILE_PATH);

            commands_preloaded_string = File.ReadAllText(FILE_PATH);
            commands_preloaded_list = JsonConvert.DeserializeObject<List<Command>>(commands_preloaded_string);

            if (commands_preloaded_list != null)
            {
                foreach (Command command in commands_preloaded_list)
                {
                    Load(command);
                }
            }

            BotDebug.BlockEnd();
        }

        #region Load commands

        /// <summary>
        /// Loads a <see cref="Command"/> into the <see cref="commands_list"/> and the <see cref="commands_dictionary"/> to be used in real time.
        /// </summary>
        /// <param name="command">The command to load.</param>
        private void Load(Command command)
        {
            BotDebug.BlankLine();

            BotDebug.SubHeader("Loading command...");

            if (!CheckSyntax(command))
            {
                BotDebug.Error(DebugMethod.Load, DebugObject.Command, DebugError.Syntax);
                BotDebug.PrintLine(nameof(command.key), command.key);
                BotDebug.PrintLine(nameof(command.response), command.response);

                return;
            }

            if (Exists(command.key))
            {
                BotDebug.Error(DebugMethod.Load, DebugObject.Command, DebugError.ExistYes);
                BotDebug.PrintLine(nameof(command.key), command.key);

                return;
            }

            if (!command.permission.CheckEnumRange<UserType>())
            {
                BotDebug.SyntaxError(DebugObject.Command, DebugObject.Setting, SyntaxError.EnumRange, Enum.GetNames(typeof(UserType)).Length);
                BotDebug.PrintLine(nameof(command.permission), command.permission.ToString());
                BotDebug.PrintLine(nameof(command.permission) + " set to " + UserType.viewer.ToString());

                command.permission = UserType.viewer;
            }            

            try
            {
                commands_list.Add(command);
                commands_dictionary.Add(command.key, command);

                BotDebug.Success(DebugMethod.Load, DebugObject.Command, command.key);
                BotDebug.PrintObject(command);
            }
            catch (Exception exception)
            {
                BotDebug.Error(DebugMethod.Load, DebugObject.Command, DebugError.Exception);
                BotDebug.PrintLine(nameof(command.key), command.key);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }
        }

        #endregion

        #region Add, Edit, and Remove commands

        /// <summary>
        /// Modify the commands by adding, editting, or removing commands.
        /// </summary>
        /// <param name="variables">Used for parsing the command for possible variables.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        public void Modify(Variables variables, Message message)
        {
            string temp = ParseCommandString(message),
                   key = temp.TextBefore(" ");

            message.body = temp.TextAfter(" ");

            try
            {
                switch (key)
                {
                    case "!add":
                        Add(variables, message);
                        break;
                    case "!edit":
                        Edit(variables, message);
                        break;
                    case "!remove":
                        Remove(variables, message);
                        break;
                    default:
                        break;
                }
            } 
            catch(Exception exception)
            {
                BotDebug.Error(DebugMethod.Modify, DebugObject.Command, DebugError.Exception);
                BotDebug.PrintLine(nameof(exception), exception.Message);
                BotDebug.PrintLine(nameof(temp), temp);
                BotDebug.PrintLine(nameof(key), key);
                BotDebug.PrintLine(nameof(message.body), message.body);
            }            
        }

        /// <summary>
        /// Adds a command with a given reponse into the <see cref="commands_dictionary"/> in real time.
        /// </summary>
        /// <param name="command">Command to be added.</param>
        /// <param name="variables">Parses the response for any valid variables and loads them into the <see cref="Variables.variables_dictionary"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        private void Add(Variables variables, Message message)
        {
            BotDebug.BlankLine();
            BotDebug.SubHeader("Adding command...");

            Command command = MessageToCommand(DebugMethod.Add, message, variables);

            if (command == default(Command))
            {
                return;
            }

            if (!CheckSyntax(command))
            {
                Notify.Error(DebugMethod.Add, DebugObject.Command, command.key, DebugError.Syntax, message);

                BotDebug.Error(DebugMethod.Add, DebugObject.Command, DebugError.Syntax);
                BotDebug.PrintLine(nameof(command.key), command.key);
                BotDebug.PrintLine(nameof(command.response), command.response);

                return;
            }

            if (Exists(command.key))
            {
                Notify.Error(DebugMethod.Add, DebugObject.Command, command.key, DebugError.ExistYes, message);

                BotDebug.Error(DebugMethod.Add, DebugObject.Command, DebugError.ExistYes);
                BotDebug.PrintLine(nameof(command.key), command.key);

                return;
            }

            if (!command.permission.CheckEnumRange<UserType>())
            {
                int enum_size = Enum.GetNames(typeof(UserType)).Length;

                BotDebug.SyntaxError(DebugObject.Command, DebugObject.Setting, SyntaxError.EnumRange, Enum.GetNames(typeof(UserType)).Length);
                BotDebug.PrintLine(nameof(command.permission), ((int)command.permission).ToString());
                BotDebug.PrintLine(nameof(command.permission) + "set to " + UserType.viewer.ToString());

                command.permission = UserType.viewer;
            }

            try
            {
                commands_list.Add(command);
                commands_dictionary.Add(command.key, command);

                JsonConvert.SerializeObject(commands_list, Formatting.Indented).OverrideFile(FILE_PATH);

                Notify.Success(DebugMethod.Add, DebugObject.Command, command.key, message);

                BotDebug.Success(DebugMethod.Add, DebugObject.Command, command.key);
                BotDebug.PrintObject(command);
            }
            catch (Exception exception)
            {
                Notify.Error(DebugMethod.Add, DebugObject.Command, command.key, DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Add, DebugObject.Command, DebugError.Exception);
                BotDebug.PrintLine(nameof(command.key), command.key);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Edits the response of a given command in the <see cref="commands_dictionary"/> in real time.
        /// </summary>
        /// <param name="command_model">Command to be edited.</param>
        /// <param name="variables">Parses the response for any valid variables and loads them into the <see cref="Variables.variables_dictionary"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        private void Edit(Variables variables, Message message)
        {
            BotDebug.BlankLine();
            BotDebug.SubHeader("Editing command...");

            Command command_model = MessageToCommand(DebugMethod.Edit, message, variables);

            if (command_model == default(Command))
            {
                return;
            }

            string command_preserialized = ParseCommandString(message).PreserializeAs<string>();        
                        
            if (!Exists(command_model.key))
            {
                Notify.Error(DebugMethod.Edit, DebugObject.Command, command_model.key, DebugError.ExistNo, message);

                BotDebug.Error(DebugMethod.Edit, DebugObject.Command, DebugError.ExistNo);
                BotDebug.PrintLine(nameof(command_model.key), command_model.key);

                return;
            }

            Command command = new Command
            {
                key = command_model.key,
                response = command_preserialized.Contains("\"response\":") ? command_model.response : commands_dictionary[command_model.key].response,
                permanent = command_preserialized.Contains("\"permanent\":") ? command_model.permanent : commands_dictionary[command_model.key].permanent,
                permission = command_preserialized.Contains("\"permission\":") ? command_model.permission : commands_dictionary[command_model.key].permission,
                type = command_preserialized.Contains("\"type\":") ? command_model.type : commands_dictionary[command_model.key].type,
                cooldown = command_preserialized.Contains("\"cooldown\":") ? command_model.cooldown : commands_dictionary[command_model.key].cooldown
            };

            if (!CheckSyntax(command))
            {
                Notify.Error(DebugMethod.Edit, DebugObject.Command, command.key, DebugError.Syntax, message);

                BotDebug.Error(DebugMethod.Edit, DebugObject.Command, DebugError.Syntax);
                BotDebug.PrintLine(nameof(command.key), command.key);
                BotDebug.PrintLine(nameof(command.response), command.response);                               

                return;
            }

            if (!command.permission.CheckEnumRange<UserType>())
            {
                int enum_size = Enum.GetNames(typeof(UserType)).Length - 1;

                BotDebug.SyntaxError(DebugObject.Command, DebugObject.Setting, SyntaxError.EnumRange, Enum.GetNames(typeof(UserType)).Length);
                BotDebug.PrintLine(nameof(command.permission), ((int)command.permission).ToString());
                BotDebug.PrintLine(nameof(command.permission) + "set to " + commands_dictionary[command_model.key].permission.ToString());

                command.permission = commands_dictionary[command_model.key].permission;
            }

            try
            {
                commands_list.Remove(commands_dictionary[command.key]);
                commands_list.Add(command);

                commands_dictionary[command.key] = command;

                JsonConvert.SerializeObject(commands_list, Formatting.Indented).OverrideFile(FILE_PATH);

                Notify.Success(DebugMethod.Edit, DebugObject.Command, command.key, message);

                BotDebug.Success(DebugMethod.Edit, DebugObject.Command, command.key);
                BotDebug.PrintObject(command);
            }
            catch (Exception exception)
            {
                Notify.Error(DebugMethod.Edit, DebugObject.Command, command.key, DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Edit, DebugObject.Command, DebugError.Exception);
                BotDebug.PrintLine(nameof(command.key), command.key);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Removed the specified command from the <see cref="commands_dictionary"/> dictionary in real time.
        /// </summary>
        /// <param name="command">Command to be removed.</param>
        /// <param name="variables">Required to create a command model in order for the command to be removed properly.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        private void Remove(Variables variables, Message message)
        {
            BotDebug.BlankLine();
            BotDebug.SubHeader("Removing command...");

            Command command = MessageToCommand(DebugMethod.Remove, message, variables);

            if (command == default(Command))
            {
                return;
            }

            if (!Exists(command.key))
            {
                Notify.Error(DebugMethod.Remove, DebugObject.Command, command.key, DebugError.ExistNo, message);

                BotDebug.Error(DebugMethod.Remove, DebugObject.Command, DebugError.ExistNo);
                BotDebug.PrintLine(nameof(command.key), command.key);               

                return;
            }

            if (isPermanent(command.key))
            {
                Notify.Error(DebugMethod.Remove, DebugObject.Command, command.key, DebugError.Permanent, message);

                BotDebug.Error(DebugMethod.Remove, DebugObject.Command, DebugError.Permanent);
                BotDebug.PrintLine(nameof(command.key), command.key);

                return;
            }

            try
            {
                commands_list.Remove(commands_dictionary[command.key]);
                commands_dictionary.Remove(command.key);                

                JsonConvert.SerializeObject(commands_list, Formatting.Indented).OverrideFile(FILE_PATH);

                Notify.Success(DebugMethod.Remove, DebugObject.Command, command.key, message);

                BotDebug.Success(DebugMethod.Remove, DebugObject.Command, command.key);
                BotDebug.PrintObject(command);
            }
            catch (Exception exception)
            {
                Notify.Error(DebugMethod.Remove, DebugObject.Command, command.key, DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Remove, DebugObject.Command, DebugError.Exception);
                BotDebug.PrintLine(nameof(command.key), command.key);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }
        }

        #endregion

        #region Extract command information

        /// <summary>
        /// Parses through a string and checks to see if if the string contains a command.
        /// </summary>
        /// <param name="message">The string to be parsed and checked for a command.</param>
        /// <returns></returns>
        public Command ExtractCommand(string message)
        {
            string[] body = message.StringToArray<string>(' ');

            foreach (string word in body)
            {
                if (commands_dictionary.ContainsKey(word))
                {
                    return commands_dictionary[word];
                }
            }

            return default(Command);
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
            if (!commands_dictionary.ContainsKey(command))
            {
                return string.Empty;
            }

            string response = commands_dictionary[command].response;

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

        /// <summary>
        /// Returns a list of all the commands in a single string
        /// </summary>
        /// <returns></returns>
        public string GetCommands()
        {
            string commands = string.Empty;

            foreach(Command command in commands_list)
            {
                commands += command.key + " ";
            }

            return commands;
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
        /// <param name="command">Command to be checked for proper syntax.</param>
        /// <returns></returns>
        private bool CheckSyntax(Command command)
        {
            if (!command.key.CheckString())
            {
                BotDebug.SyntaxError(DebugObject.Command, DebugObject.Command, SyntaxError.Null);
                BotDebug.PrintLine(nameof(command.key), "null");

                return false;
            }

            if (!command.response.CheckString())
            {
                BotDebug.SyntaxError(DebugObject.Command, DebugObject.Response, SyntaxError.Null);
                BotDebug.PrintLine(nameof(command.key), command.key);
                BotDebug.PrintLine(nameof(command.response), "null");

                return false;
            }

            if (!command.key.StartsWith("!"))
            {
                BotDebug.SyntaxError(DebugObject.Command, DebugObject.Command, SyntaxError.EexclamationPoint);
                BotDebug.PrintLine(nameof(command.key), command.key);

                return false;
            }


            //command needs at least one character after "!"
            if (command.key.Length < 2)
            {
                BotDebug.SyntaxError(DebugObject.Command, DebugObject.Command, SyntaxError.Length);
                BotDebug.PrintLine(nameof(command.key), command.key);
                BotDebug.PrintLine("length", command.key.Length.ToString());
                BotDebug.PrintLine("minimum length:", "2");

                return false;
            }

            //check for illegal characters
            if (command.key.Contains("{") || command.key.Contains("}"))
            {
                BotDebug.SyntaxError(DebugObject.Command, DebugObject.Command, SyntaxError.BracketsNo);
                BotDebug.PrintLine(nameof(command.key), command.key);

                return false;
            }

            if (command.key.Contains("[") || command.key.Contains("]"))
            {
                BotDebug.SyntaxError(DebugObject.Command, DebugObject.Command, SyntaxError.SquareBracketsNo);
                BotDebug.PrintLine(nameof(command.key), command.key);

                return false;
            }

            if (command.key.Contains("(") || command.key.Contains(")"))
            {
                BotDebug.SyntaxError(DebugObject.Command, DebugObject.Command, SyntaxError.ParenthesisNo);
                BotDebug.PrintLine(nameof(command.key), command.key);

                return false;
            }            

            return true;
        }

        #endregion

        #region Command wrappers               

        /// <summary>
        /// Gets the name of the current song playing.
        /// Reads the name from the file path specified from the <code>!music</code> response.
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
                    return string.Empty;
                }
            }
            catch (Exception exception)
            {
                BotDebug.Error("Failed to get current song: unkown error");
                BotDebug.PrintLine(nameof(exception), exception.Message);

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
                   how_long = user + " is not following " + channel;

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

                how_long = user + " has been following " + channel + " since " + date_followed.ToShortDateString() + " PogChamp";
            }
            catch (Exception exception)
            {
                how_long = "Could not retrieve follow time at this time, " + user + " BibleThump";

                BotDebug.Error("Failed to get how long " + user + " has been following " + channel);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }

            return how_long;
        }        

        #endregion

        #region String parsing and utility functions

        /// <summary>
        /// Converts a message recieved from Twitch into a <see cref="Command"/> and returns the command.
        /// Returns null if the message could not be converted to a <see cref="Command"/>.
        /// </summary>
        /// <param name="method">The type of operation being performed.</param>
        /// <param name="variables">Parses the response for any valid variables and loads them into the <see cref="Variables.variables_dictionary"/> dictionary.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <returns></returns>
        private Command MessageToCommand(DebugMethod method, Message message, Variables variables)
        {
            string command_string;

            Variable[] variable_array;

            command_string = message.body;
            command_string = variables.ExtractVariables(command_string, message, out variable_array);
            command_string = command_string.PreserializeAs<string>();     

            try
            {
                Command command = JsonConvert.DeserializeObject<Command>(command_string);

                BotDebug.Success(DebugMethod.Serialize, DebugObject.Command, command.key);
                BotDebug.PrintObject(command);

                foreach (Variable variable in variable_array)
                {
                    BotDebug.BlankLine();
                    BotDebug.SubHeader("Adding variable...");

                    variables.Add(variable, message);
                }

                return command; 
            }
            catch (Exception exception)
            {
                Notify.Error(method, DebugObject.Command, command_string, DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Serialize, DebugObject.Command, DebugError.Exception);
                BotDebug.Error(method, DebugObject.Command, DebugError.Null);
                BotDebug.PrintLine(nameof(command_string), command_string);
                BotDebug.PrintLine(nameof(exception), exception.Message);

                return default(Command);
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

                BotDebug.Success(DebugMethod.ParseKVP, DebugObject.Command, message.body);
                BotDebug.PrintLine(nameof(key), key);
                BotDebug.PrintLine(nameof(value), value);
            }
            catch (Exception exception)
            {
                BotDebug.Error(DebugMethod.ParseKVP, DebugObject.Command, DebugError.Exception);
                BotDebug.PrintLine(nameof(message.body), message.body);
                BotDebug.PrintLine(nameof(exception), exception.Message);
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
            string result = string.Empty;

            try
            {
                result = message.body.TextAfter(message.command.key);
                result = result.RemoveWhiteSpace(WhiteSpace.Left);

                BotDebug.Success(DebugMethod.ParseString, DebugObject.Command, message.body);
                BotDebug.PrintLine(nameof(result), result);
            }
            catch (Exception exception)
            {
                BotDebug.Error(DebugMethod.ParseString, DebugObject.Command, DebugError.Exception);
                BotDebug.PrintLine(nameof(message.body), message.body);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }

            return result;
        }

        /// <summary>
        /// Downloads the text from a web page.
        /// </summary>
        /// <param name="url">URL of the web page.</param>
        /// <returns></returns>
        private string GetWebText(string url)
        {
            WebClient web_client = new WebClient();

            return web_client.DownloadString(url);
        }

        /// <summary>
        /// Resets the last time a command was used to the current time.
        /// </summary>
        /// <param name="command">The command to reset the time last used for.</param>
        public void ResetLastUsed(Command command)
        {
            if (!Exists(command.key))
            {
                BotDebug.Error(DebugMethod.Edit, DebugObject.Command, DebugError.ExistNo);
                BotDebug.PrintLine("Failed to set last time used");
            }

            commands_dictionary[command.key].last_used = DateTime.Now;
        }

        #endregion
    }
}