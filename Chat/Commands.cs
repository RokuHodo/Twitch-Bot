using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using Newtonsoft.Json;

using TwitchBot.Clients;
using TwitchBot.Debugger;
using TwitchBot.Enums.Chat;
using TwitchBot.Enums.Extensions;
using TwitchBot.Extensions;
using TwitchBot.Extensions.Files;
using TwitchBot.Helpers;
using TwitchBot.Models.Bot.Chat;
using TwitchBot.Models.TwitchAPI;

namespace TwitchBot.Chat
{
    class Commands
    {
        readonly string FILE_PATH = Environment.CurrentDirectory + "/JSON/Chat/Commands.json";

        List<Command> commands_list;

        Dictionary<string, Command> commands_dictionary;

        public Commands(Variables variables)
        {
            commands_list = new List<Command>();
            commands_dictionary = new Dictionary<string, Command>();            

            DebugBot.BlankLine();

            DebugBot.BlockBegin();
            DebugBot.Header("Loading Commands");
            DebugBot.PrintLine("File path:", FILE_PATH);

            string commands_preloaded_string = File.ReadAllText(FILE_PATH);
            List<Command>  commands_preloaded_list = JsonConvert.DeserializeObject<List<Command>>(commands_preloaded_string);

            if (commands_preloaded_list != null)
            {
                foreach (Command command in commands_preloaded_list)
                {
                    Load(command);
                }
            }

            DebugBot.BlockEnd();
        }

        #region Load commands

        /// <summary>
        /// Loads a <see cref="Command"/> into the <see cref="commands_list"/> and the <see cref="commands_dictionary"/> to be used in real time.
        /// </summary>
        /// <param name="command">The command to load.</param>
        private void Load(Command command)
        {
            DebugBot.BlankLine();

            DebugBot.SubHeader("Loading command...");

            if (!CheckSyntax(DebugMethod.LOAD, command))
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.LOAD, nameof(command), DebugError.NORMAL_SYNTAX);
                DebugBot.PrintObject(command.key);

                return;
            }

            if (Exists(command.key))
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.LOAD, nameof(command), DebugError.NORMAL_EXISTS_YES);
                DebugBot.PrintLine(nameof(command.key), command.key);

                return;
            }

            if (!command.permission.CheckEnumRange<UserType>())
            {
                DebugBot.PrintLine(DebugMessageType.WARNING, DebugMethod.LOAD, nameof(command.permission) ,DebugError.NORMAL_OUT_OF_BOUNDS);
                DebugBot.PrintLine(nameof(command.permission), command.permission.ToString());
                DebugBot.PrintLine("Setting the " + nameof(command.permission) + " to " + UserType.viewer.ToString());

                command.permission = UserType.viewer;
            }            

            try
            {
                commands_list.Add(command);
                commands_dictionary.Add(command.key, command);

                DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.LOAD, nameof(command));
                DebugBot.PrintObject(command);
            }
            catch (Exception exception)
            {
                DebugBot.PrintLine(DebugMessageType.WARNING, DebugMethod.LOAD, nameof(command), DebugError.NORMAL_UNKNOWN);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        #endregion

        #region Add, Edit, and Remove commands

        /// <summary>
        /// Modify the commands by adding, editting, or removing commands.
        /// </summary>
        /// <param name="variables">Used for parsing the command for possible variables.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        public void Modify(Variables variables, TwitchMessage message)
        {
            string temp = ParseAfterCommand(message),
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
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.MODIFY, "command", DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);

                DebugBot.PrintLine(nameof(temp), temp);
                DebugBot.PrintLine(nameof(key), key);
                DebugBot.PrintLine(nameof(message.body), message.body);
            }           
        }

        /// <summary>
        /// Adds a command with a given reponse into the <see cref="commands_dictionary"/> in real time.
        /// </summary>
        /// <param name="command">Command to be added.</param>
        /// <param name="variables">Parses the response for any valid variables and loads them into the <see cref="Variables.variables_dictionary"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        private void Add(Variables variables, TwitchMessage message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Adding command...");

            Command command = MessageToCommand(message, variables);

            if (command == default(Command))
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.ADD, message, nameof(command), message.body, DebugError.NORMAL_SERIALIZE);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.ADD, nameof(command), DebugError.NORMAL_SERIALIZE);
                DebugBot.PrintLine(nameof(message.body), message.body);

                return;
            }

            if (!CheckSyntax(DebugMethod.ADD, command))
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.ADD, message, nameof(command), command.key, DebugError.NORMAL_SYNTAX);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.ADD, nameof(command), DebugError.NORMAL_SYNTAX);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine(nameof(command.response), command.response);

                return;
            }

            if (Exists(command.key))
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.ADD, message, nameof(command), command.key, DebugError.NORMAL_EXISTS_YES);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.ADD, nameof(command), DebugError.NORMAL_EXISTS_YES);
                DebugBot.PrintLine(nameof(command.key), command.key);

                return;
            }

            if (!command.permission.CheckEnumRange<UserType>())
            {
                int enum_size = Enum.GetNames(typeof(UserType)).Length;

                DebugBot.PrintLine(DebugMessageType.WARNING, DebugMethod.ADD, nameof(command.permission), DebugError.NORMAL_OUT_OF_BOUNDS);
                DebugBot.PrintLine(nameof(command.permission), command.permission.ToString());
                DebugBot.PrintLine("Setting the " + nameof(command.permission) + " to " + UserType.viewer.ToString());

                command.permission = UserType.viewer;
            }

            try
            {
                commands_list.Add(command);
                commands_dictionary.Add(command.key, command);

                JsonConvert.SerializeObject(commands_list, Formatting.Indented).OverrideFile(FILE_PATH);

                Notify.Enqueue(DebugMessageType.SUCCESS, DebugMethod.ADD, message, nameof(command), command.key);

                DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.ADD, nameof(command));
                DebugBot.PrintObject(command);
            }
            catch (Exception exception)
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.ADD, message, nameof(command), command.key, DebugError.NORMAL_EXCEPTION);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.ADD, nameof(command), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Edits the response of a given command in the <see cref="commands_dictionary"/> in real time.
        /// </summary>
        /// <param name="command_model">Command to be edited.</param>
        /// <param name="variables">Parses the response for any valid variables and loads them into the <see cref="Variables.variables_dictionary"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        private void Edit(Variables variables, TwitchMessage message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Editing command...");

            Command command_model = MessageToCommand(message, variables);

            if (command_model == default(Command))
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.EDIT, message, nameof(command_model), message.body, DebugError.NORMAL_SERIALIZE);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.EDIT, nameof(command_model), DebugError.NORMAL_SERIALIZE);
                DebugBot.PrintLine(nameof(message.body), message.body);

                return;
            }                
                        
            if (!Exists(command_model.key))
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.EDIT, message, nameof(command_model), command_model.key, DebugError.NORMAL_EXISTS_NO);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.EDIT, nameof(command_model), DebugError.NORMAL_EXISTS_NO);
                DebugBot.PrintLine(nameof(command_model.key), command_model.key);

                return;
            }

            string command_preserialized = message.body.PreserializeAs<string>();    

            Command command = new Command
            {
                key = command_model.key,
                response = command_preserialized.Contains("\"response\":") ? command_model.response : commands_dictionary[command_model.key].response,
                permanent = command_preserialized.Contains("\"permanent\":") ? command_model.permanent : commands_dictionary[command_model.key].permanent,
                permission = command_preserialized.Contains("\"permission\":") ? command_model.permission : commands_dictionary[command_model.key].permission,
                type = command_preserialized.Contains("\"type\":") ? command_model.type : commands_dictionary[command_model.key].type,
                cooldown = command_preserialized.Contains("\"cooldown\":") ? command_model.cooldown : commands_dictionary[command_model.key].cooldown
            };

            if (!CheckSyntax(DebugMethod.EDIT, command))
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.EDIT, message, nameof(command), command.key, DebugError.NORMAL_SYNTAX);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.EDIT, nameof(command), DebugError.NORMAL_SYNTAX);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine(nameof(command.response), command.response);                               

                return;
            }

            if (!command.permission.CheckEnumRange<UserType>())
            {
                int enum_size = Enum.GetNames(typeof(UserType)).Length - 1;

                DebugBot.PrintLine(DebugMessageType.WARNING, DebugMethod.EDIT, nameof(command.permission), DebugError.NORMAL_OUT_OF_BOUNDS);
                DebugBot.PrintLine(nameof(command.permission), command.permission.ToString());
                DebugBot.PrintLine("Setting the " + nameof(command.permission) + " to " + UserType.viewer.ToString());

                command.permission = commands_dictionary[command_model.key].permission;
            }

            try
            {
                commands_list.Remove(commands_dictionary[command.key]);
                commands_list.Add(command);

                commands_dictionary[command.key] = command;

                JsonConvert.SerializeObject(commands_list, Formatting.Indented).OverrideFile(FILE_PATH);

                Notify.Enqueue(DebugMessageType.SUCCESS, DebugMethod.EDIT, message, nameof(command), command.key);

                DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.EDIT, nameof(command));
                DebugBot.PrintObject(command);
            }
            catch (Exception exception)
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.EDIT, message, nameof(command), command.key, DebugError.NORMAL_EXCEPTION);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.EDIT, nameof(command), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Removed the specified command from the <see cref="commands_dictionary"/> dictionary in real time.
        /// </summary>
        /// <param name="command">Command to be removed.</param>
        /// <param name="variables">Required to create a command model in order for the command to be removed properly.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        private void Remove(Variables variables, TwitchMessage message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Removing command...");

            Command command = MessageToCommand(message, variables);

            if (command == default(Command))
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.REMOVE, message, nameof(command), message.body, DebugError.NORMAL_SERIALIZE);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.REMOVE, nameof(command), DebugError.NORMAL_SERIALIZE);
                DebugBot.PrintLine(nameof(message.body), message.body);

                return;
            }

            if (!Exists(command.key))
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.REMOVE, message, nameof(command), command.key, DebugError.NORMAL_EXISTS_NO);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.REMOVE, nameof(command), DebugError.NORMAL_EXISTS_NO);
                DebugBot.PrintLine(nameof(command.key), command.key);               

                return;
            }

            if (isPermanent(command.key))
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.REMOVE, message, nameof(command), command.key, DebugError.NORMAL_PERMANENT);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.REMOVE, nameof(command), DebugError.NORMAL_PERMANENT);
                DebugBot.PrintLine(nameof(command.key), command.key);

                return;
            }

            try
            {
                commands_list.Remove(commands_dictionary[command.key]);
                commands_dictionary.Remove(command.key);                

                JsonConvert.SerializeObject(commands_list, Formatting.Indented).OverrideFile(FILE_PATH);

                Notify.Enqueue(DebugMessageType.SUCCESS, DebugMethod.REMOVE, message, nameof(command), command.key);

                DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.REMOVE, nameof(command));
                DebugBot.PrintObject(command);
            }
            catch (Exception exception)
            {
                Notify.Enqueue(DebugMessageType.ERROR, DebugMethod.REMOVE, message, nameof(command), command.key, DebugError.NORMAL_EXCEPTION);

                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.REMOVE, nameof(command), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine(nameof(exception), exception.Message);
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
            if (!message.CheckString())
            {
                return default(Command);
            }

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
            if (!command.CheckString())
            {
                return false;
            }

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
        private bool CheckSyntax(DebugMethod method, Command command)
        {
            bool pass = true;

            if (!command.key.CheckString())
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, method, nameof(command), DebugError.SYNTAX_NULL);
                DebugBot.PrintLine(nameof(command.key), "null");

                pass = false;
            }

            if (!command.response.CheckString())
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, method, nameof(command), DebugError.SYNTAX_NULL);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine(nameof(command.response), "null");

                pass = false;
            }

            if (!command.key.StartsWith("!"))
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, method, nameof(command), DebugError.SYNTAX_EXCLAMATION_POINT_LEAD_YES);
                DebugBot.PrintLine(nameof(command.key), command.key);

                pass = false;
            }


            //command needs at least one character after "!"
            if (command.key.Length < 2)
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, method, nameof(command), DebugError.SYNTAX_LENGTH);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine("length", command.key.Length.ToString());
                DebugBot.PrintLine("minimum length:", "2");

                pass = false;
            }

            //check for illegal characters
            if (command.key.Contains("{") || command.key.Contains("}"))
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, method, nameof(command), DebugError.SYNTAX_BRACKETS_NO);
                DebugBot.PrintLine(nameof(command.key), command.key);

                pass = false;
            }

            if (command.key.Contains("[") || command.key.Contains("]"))
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, method, nameof(command), DebugError.SYNTAX_SQUARE_BRACKETS_NO);
                DebugBot.PrintLine(nameof(command.key), command.key);

                pass = false;
            }

            if (command.key.Contains("(") || command.key.Contains(")"))
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, method, nameof(command), DebugError.SYNTAX_PARENTHESIS_NO);
                DebugBot.PrintLine(nameof(command.key), command.key);

                pass = false;
            }            

            return pass;
        }

        #endregion

        #region Command wrappers               

        /// <summary>
        /// Gives a shout out to a channel.
        /// Returns empty if the channel doesn't exist.
        /// </summary>
        /// <param name="message"><see cref="TwitchMessage"/> containing the channel to shout out.</param>
        /// <param name="client">The client to get the channel info.</param>
        /// <returns></returns>
        public string ShoutOut(TwitchMessage message, TwitchClientOAuth client)
        {
            string name = ParseAfterCommand(message),
                   shoutout_message = string.Empty;

            Channel channel = client.GetChannel(name.ToLower());

            if (!channel.display_name.CheckString())
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.GET, nameof(channel), DebugError.NORMAL_NULL);

                return shoutout_message;
            }

            shoutout_message = "Go check out " + channel.display_name + " over at https://www.twitch.tv/" + channel.name + " !";

            if (channel.game.CheckString())
            {
                shoutout_message += " They were last pplaying " + channel.game + " on " + channel.updated_at.ToLocalTime().ToLongDateString() + ".";            
            }

            return shoutout_message;
        }

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
                DebugBot.PrintLine(DebugMessageType.ERROR, "Failed to get current song: unkown error");
                DebugBot.PrintLine(nameof(exception), exception.Message);

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

                DebugBot.PrintLine(DebugMessageType.ERROR, "Failed to get how long " + user + " has been following " + channel);
                DebugBot.PrintLine(nameof(exception), exception.Message);
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
        private Command MessageToCommand(TwitchMessage message, Variables variables)
        {
            string command_string = message.body;

            Variable[] variable_array;

            //first, search and extract any variable before ther message is serealzied 
            command_string = variables.ExtractVariables(command_string, message, out variable_array);

            //now format the message body in a way that can be serialized into JSON
            command_string = command_string.PreserializeAs<string>();     

            try
            {
                Command command = JsonConvert.DeserializeObject<Command>(command_string);

                DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.SERIALIZE, nameof(command));
                DebugBot.PrintObject(command);
                
                foreach (Variable variable in variable_array)
                {
                    DebugBot.BlankLine();
                    DebugBot.SubHeader("Adding variable...");

                    variables.Add(variable, message);
                }

                return command; 
            }
            catch (Exception exception)
            {                
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.SERIALIZE, nameof(command_string), DebugError.NORMAL_EXCEPTION);              
                DebugBot.PrintLine(nameof(command_string), command_string);
                DebugBot.PrintLine(nameof(exception), exception.Message);

                return default(Command);
            }
        }

        /// <summary>
        /// Parses the body of a <see cref="TwitchMessage"/> after the command and returns a <see cref="string"/>.
        /// </summary>
        /// <param name="message">Contains the body and command of the message that is parsed.</param>
        /// <returns></returns>
        public string ParseAfterCommand(TwitchMessage message)
        {
            string result = string.Empty;

            try
            {
                result = message.body.TextAfter(message.command.key);
                result = result.RemoveWhiteSpace(WhiteSpace.Left);

                DebugBot.PrintLine(DebugMessageType.SUCCESS, DebugMethod.PARSE, nameof(message) + " after the command");
                DebugBot.PrintLine(nameof(result), result);
            }
            catch (Exception exception)
            {
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.PARSE, nameof(message) + " after the command", DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(message.body), message.body);
                DebugBot.PrintLine(nameof(exception), exception.Message);
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
                DebugBot.PrintLine(DebugMessageType.ERROR, DebugMethod.EDIT, nameof(command), DebugError.NORMAL_EXISTS_NO);
                DebugBot.PrintLine("Failed to set last time used");
            }

            commands_dictionary[command.key].last_used = DateTime.Now;
        }

        #endregion
    }
}