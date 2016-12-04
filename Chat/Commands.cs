using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using Newtonsoft.Json;

using TwitchBot.Clients;
using TwitchBot.Debugger;
using TwitchBot.Enums.Chat;
using TwitchBot.Enums.Debugger;
using TwitchBot.Enums.Extensions;
using TwitchBot.Extensions;
using TwitchBot.Extensions.Files;
using TwitchBot.Messages;
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

            DebugBot.Notify("Loading Commands");
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
        }

        #region Load commands

        /// <summary>
        /// Loads all the <see cref="Command"/>s from the <see cref="FILE_PATH"/>.
        /// </summary>
        private void Load(Command command)
        {
            DebugBot.BlankLine();

            DebugBot.SubHeader("Loading command...");

            if (!CheckSyntax(DebugMethod.LOAD, command))
            {
                DebugBot.Error(DebugMethod.LOAD, nameof(command), DebugError.NORMAL_SYNTAX);
                DebugBot.PrintObject(command.key);

                return;
            }

            if (Exists(command.key))
            {
                DebugBot.Error(DebugMethod.LOAD, nameof(command), DebugError.NORMAL_EXISTS_YES);
                DebugBot.PrintLine(nameof(command.key), command.key);

                return;
            }

            if (!command.permission.CheckEnumRange<UserType>())
            {
                //the value specified for user-type was out of range, set it to the default
                DebugBot.Warning(nameof(command.permission) + ": " + DebugError.NORMAL_OUT_OF_BOUNDS);
                DebugBot.PrintLine(nameof(command.permission), command.permission.ToString());
                DebugBot.PrintLine("Setting the " + nameof(command.permission) + " to " + UserType.viewer.ToString());

                command.permission = UserType.viewer;
            }            

            try
            {
                commands_list.Add(command);
                commands_dictionary.Add(command.key, command);

                DebugBot.Success(DebugMethod.LOAD, nameof(command));
                DebugBot.PrintObject(command);
            }
            catch (Exception exception)
            {
                DebugBot.Error(DebugMethod.LOAD, nameof(command), DebugError.NORMAL_UNKNOWN);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        #endregion

        #region Add, Edit, and Remove commands

        /// <summary>
        /// Modify commands by adding, editting, or removing commands.
        /// </summary>        
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
                DebugBot.Error(DebugMethod.MODIFY, "command", DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);

                DebugBot.PrintLine(nameof(temp), temp);
                DebugBot.PrintLine(nameof(key), key);
                DebugBot.PrintLine(nameof(message.body), message.body);
            }           
        }

        /// <summary>
        /// Adds a <see cref="Command"/> at run time to be used in real time without needing to re-launch the bot.
        /// </summary>        
        private void Add(Variables variables, TwitchMessage message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Adding command...");

            Command command = MessageToCommand(message, variables);

            //the chat message could not be deserialized into a command
            if (command == default(Command))
            {
                TwitchNotify.Error(DebugMethod.ADD, message, nameof(command), message.body, DebugError.NORMAL_DESERIALIZE);

                DebugBot.Error(DebugMethod.ADD, nameof(command), DebugError.NORMAL_DESERIALIZE);
                DebugBot.PrintLine(nameof(message.body), message.body);

                return;
            }

            if (!CheckSyntax(DebugMethod.ADD, command))
            {
                TwitchNotify.Error(DebugMethod.ADD, message, nameof(command), command.key, DebugError.NORMAL_SYNTAX);

                DebugBot.Error(DebugMethod.ADD, nameof(command), DebugError.NORMAL_SYNTAX);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine(nameof(command.response), command.response);

                return;
            }

            if (Exists(command.key))
            {
                TwitchNotify.Error(DebugMethod.ADD, message, nameof(command), command.key, DebugError.NORMAL_EXISTS_YES);

                DebugBot.Error(DebugMethod.ADD, nameof(command), DebugError.NORMAL_EXISTS_YES);
                DebugBot.PrintLine(nameof(command.key), command.key);

                return;
            }
            
            if (!command.permission.CheckEnumRange<UserType>())
            {
                int enum_size = Enum.GetNames(typeof(UserType)).Length;

                //the value specified for user-type was out of range, set it to the default
                DebugBot.Warning(nameof(command.permission) + ": " + DebugError.NORMAL_OUT_OF_BOUNDS);
                DebugBot.PrintLine(nameof(command.permission), command.permission.ToString());
                DebugBot.PrintLine("Setting the " + nameof(command.permission) + " to " + UserType.viewer.ToString());

                command.permission = UserType.viewer;
            }

            try
            {
                commands_list.Add(command);
                commands_dictionary.Add(command.key, command);

                JsonConvert.SerializeObject(commands_list, Formatting.Indented).OverrideFile(FILE_PATH);

                TwitchNotify.Success(DebugMethod.ADD, message, nameof(command), command.key);

                DebugBot.Success(DebugMethod.ADD, nameof(command));
                DebugBot.PrintObject(command);
            }
            catch (Exception exception)
            {
                TwitchNotify.Error(DebugMethod.ADD, message, nameof(command), command.key, DebugError.NORMAL_EXCEPTION);

                DebugBot.Error(DebugMethod.ADD, nameof(command), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Edits a pre-existing <see cref="Command"/> at run time without needing to re-launch the bot for the changes to take affect.
        /// </summary> 
        private void Edit(Variables variables, TwitchMessage message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Editing command...");

            Command command_model = MessageToCommand(message, variables);

            //the chat message could not be deserialized into a command
            if (command_model == default(Command))
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, nameof(command_model), message.body, DebugError.NORMAL_DESERIALIZE);

                DebugBot.Error(DebugMethod.EDIT, nameof(command_model), DebugError.NORMAL_DESERIALIZE);
                DebugBot.PrintLine(nameof(message.body), message.body);

                return;
            }                
                        
            if (!Exists(command_model.key))
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, nameof(command_model), command_model.key, DebugError.NORMAL_EXISTS_NO);

                DebugBot.Error(DebugMethod.EDIT, nameof(command_model), DebugError.NORMAL_EXISTS_NO);
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
                TwitchNotify.Error(DebugMethod.EDIT, message, nameof(command), command.key, DebugError.NORMAL_SYNTAX);

                DebugBot.Error(DebugMethod.EDIT, nameof(command), DebugError.NORMAL_SYNTAX);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine(nameof(command.response), command.response);                               

                return;
            }

            //the value specified for user-type was out of range, set it to the default
            if (!command.permission.CheckEnumRange<UserType>())
            {
                int enum_size = Enum.GetNames(typeof(UserType)).Length - 1;

                DebugBot.Warning(nameof(command.permission) + ": " + DebugError.NORMAL_OUT_OF_BOUNDS);
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

                TwitchNotify.Success(DebugMethod.EDIT, message, nameof(command), command.key);

                DebugBot.Success(DebugMethod.EDIT, nameof(command));
                DebugBot.PrintObject(command);
            }
            catch (Exception exception)
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, nameof(command), command.key, DebugError.NORMAL_EXCEPTION);

                DebugBot.Error(DebugMethod.EDIT, nameof(command), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Removes a <see cref="Command"/> at run time without needing to re-launch the bot for the changes to take affect.
        /// </summary> 
        private void Remove(Variables variables, TwitchMessage message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Removing command...");

            Command command = MessageToCommand(message, variables);

            //message body could not be deserialized into a command
            if (command == default(Command))
            {
                TwitchNotify.Error(DebugMethod.REMOVE, message, nameof(command), message.body, DebugError.NORMAL_DESERIALIZE);

                DebugBot.Error(DebugMethod.REMOVE, nameof(command), DebugError.NORMAL_DESERIALIZE);
                DebugBot.PrintLine(nameof(message.body), message.body);

                return;
            }

            if (!Exists(command.key))
            {
                TwitchNotify.Error(DebugMethod.REMOVE, message, nameof(command), command.key, DebugError.NORMAL_EXISTS_NO);

                DebugBot.Error(DebugMethod.REMOVE, nameof(command), DebugError.NORMAL_EXISTS_NO);
                DebugBot.PrintLine(nameof(command.key), command.key);               

                return;
            }

            //the command is permanent, cannot be removed
            if (command.permanent)
            {
                TwitchNotify.Error(DebugMethod.REMOVE, message, nameof(command), command.key, DebugError.NORMAL_PERMANENT);

                DebugBot.Error(DebugMethod.REMOVE, nameof(command), DebugError.NORMAL_PERMANENT);
                DebugBot.PrintLine(nameof(command.key), command.key);

                return;
            }

            try
            {
                commands_list.Remove(commands_dictionary[command.key]);
                commands_dictionary.Remove(command.key);                

                JsonConvert.SerializeObject(commands_list, Formatting.Indented).OverrideFile(FILE_PATH);

                TwitchNotify.Success(DebugMethod.REMOVE, message, nameof(command), command.key);

                DebugBot.Success(DebugMethod.REMOVE, nameof(command));
                DebugBot.PrintObject(command);
            }
            catch (Exception exception)
            {
                TwitchNotify.Error(DebugMethod.REMOVE, message, nameof(command), command.key, DebugError.NORMAL_EXCEPTION);

                DebugBot.Error(DebugMethod.REMOVE, nameof(command), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        #endregion

        #region Extract command information

        /// <summary>
        /// Parses the body of a <see cref="TwitchMessage"/> for the first valid <see cref="Command"/> key.
        /// </summary>
        public Command GetCommand(string message)
        {
            Command command = default(Command);

            if (!message.CheckString())
            {
                return command;
            }

            string[] body = message.StringToArray<string>(' ');

            foreach (string word in body)
            {
                if (commands_dictionary.ContainsKey(word))
                {
                    command = commands_dictionary[word];

                    return command;
                }
            }

            return command;
        }

        /// <summary>
        /// Gets the response for the specified <see cref="Command"/> key.
        /// Replaces any valid variables in the response with their appropriate values.
        /// </summary>
        public string GetCommandResponse(string key, Variables variables)
        {
            string response = string.Empty;

            if (!Exists(key))
            {
                return response;
            }

            response = commands_dictionary[key].response;

            //replace any variables in the response with their value
            Dictionary<string, Variable> variables_dictionary = variables.GetVariables();
            foreach (KeyValuePair<string, Variable> pair in variables_dictionary)
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
        /// Checks to see if a <see cref="Command"/> already exists based on it's key.
        /// </summary>
        public bool Exists(string key)
        {
            if (!key.CheckString())
            {
                return false;
            }

            return commands_dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Checks to see if the <see cref="Command"/> has the right syntax before being loaded or modified.
        /// </summary>
        private bool CheckSyntax(DebugMethod method, Command command)
        {
            bool pass = true;

            if (!command.key.CheckString())
            {
                DebugBot.Error(method, nameof(command), DebugError.SYNTAX_NULL);
                DebugBot.PrintLine(nameof(command.key), "null");

                pass = false;
            }

            if (!command.response.CheckString())
            {
                DebugBot.Error(method, nameof(command), DebugError.SYNTAX_NULL);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine(nameof(command.response), "null");

                pass = false;
            }

            if (!command.key.StartsWith("!"))
            {
                DebugBot.Error(method, nameof(command), DebugError.SYNTAX_EXCLAMATION_POINT_LEAD_YES);
                DebugBot.PrintLine(nameof(command.key), command.key);

                pass = false;
            }


            //command key needs at least one character after "!"
            if (command.key.Length < 2)
            {
                DebugBot.Error(method, nameof(command), DebugError.SYNTAX_LENGTH);
                DebugBot.PrintLine(nameof(command.key), command.key);
                DebugBot.PrintLine("length", command.key.Length.ToString());
                DebugBot.PrintLine("minimum length:", "2");

                pass = false;
            }

            //check for illegal characters
            if (command.key.Contains("{") || command.key.Contains("}"))
            {
                DebugBot.Error(method, nameof(command), DebugError.SYNTAX_BRACKETS_NO);
                DebugBot.PrintLine(nameof(command.key), command.key);

                pass = false;
            }

            if (command.key.Contains("[") || command.key.Contains("]"))
            {
                DebugBot.Error(method, nameof(command), DebugError.SYNTAX_SQUARE_BRACKETS_NO);
                DebugBot.PrintLine(nameof(command.key), command.key);

                pass = false;
            }

            if (command.key.Contains("(") || command.key.Contains(")"))
            {
                DebugBot.Error(method, nameof(command), DebugError.SYNTAX_PARENTHESIS_NO);
                DebugBot.PrintLine(nameof(command.key), command.key);

                pass = false;
            }            

            return pass;
        }

        #endregion

        #region Command wrappers - to be moved into .dll's using reflection

        /// <summary>
        /// Gives a shout out to a valid twitch channel.
        /// </summary>
        public string GetShoutoutMessage(TwitchMessage message, TwitchClientOAuth client)
        {
            string name = ParseAfterCommand(message),
                   shoutout_message = string.Empty;

            Channel channel = client.GetChannel(name.ToLower());

            if (!channel.display_name.CheckString())
            {
                DebugBot.Error(DebugMethod.GET, nameof(channel), DebugError.NORMAL_NULL);

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
        public string GetCurrentSong()
        {
            string song = string.Empty;

            try
            {
                song = File.ReadAllText(commands_dictionary["!music"].response);

                if (song.CheckString())
                {
                    song = "Current song: " + song;
                }
            }
            catch (Exception exception)
            {
                DebugBot.Error("Failed to get current song: unkown error");
                DebugBot.PrintLine(nameof(exception), exception.Message);

                song = "Failed to retrieve song data";
            }

            return song;
        }       

        #endregion

        #region String parsing and utility functions

        /// <summary>
        /// Converts a <see cref="TwitchMessage"/> recieved from Twitch and attempts to deserialize the body in to a <see cref="Command"/>.
        /// </summary>
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

                DebugBot.Success(DebugMethod.SERIALIZE, nameof(command));
                DebugBot.PrintObject(command);
                
                //now add any extracted variables from the command response
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
                DebugBot.Error(DebugMethod.SERIALIZE, nameof(command_string), DebugError.NORMAL_EXCEPTION);              
                DebugBot.PrintLine(nameof(command_string), command_string);
                DebugBot.PrintLine(nameof(exception), exception.Message);

                return default(Command);
            }
        }

        /// <summary>
        /// Parses the body of a <see cref="TwitchMessage"/> after the <see cref="Command"/> key and removed left padding.
        /// </summary>
        public string ParseAfterCommand(TwitchMessage message)
        {
            string result = string.Empty;

            try
            {
                result = message.body.TextAfter(message.command.key);
                result = result.RemovePadding(Padding.Left);

                DebugBot.Success(DebugMethod.PARSE, nameof(message) + " after the command");
                DebugBot.PrintLine(nameof(result), result);
            }
            catch (Exception exception)
            {
                DebugBot.Error(DebugMethod.PARSE, nameof(message) + " after the command", DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(message.body), message.body);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }

            return result;
        }

        /// <summary>
        /// Sets the last time a <see cref="Command"/> was used to the broadcaster's current time.
        /// </summary>
        public void ResetLastUsed(Command command)
        {
            if (!Exists(command.key))
            {
                DebugBot.Error(DebugMethod.EDIT, nameof(command), DebugError.NORMAL_EXISTS_NO);
                DebugBot.PrintLine("Failed to set last time used");
            }

            commands_dictionary[command.key].last_used = DateTime.Now;
        }

        #endregion
    }
}