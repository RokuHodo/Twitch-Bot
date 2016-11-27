using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

using TwitchBot.Debugger;
using TwitchBot.Enums.Debugger;
using TwitchBot.Enums.Extensions;
using TwitchBot.Extensions;
using TwitchBot.Extensions.Files;
using TwitchBot.Models.Bot.Chat;
using TwitchBot.Parser;

namespace TwitchBot.Chat
{
    class Variables
    {
        char lower_variable_indicator = '[',
             upper_variable_indicator = ']',
             lower_variable_search = '(',
             upper_variable_search = ')';

        readonly string file_path = Environment.CurrentDirectory + "/JSON/Chat/Variables.json";

        List<Variable> variables_list;

        Dictionary<string, Variable> variables_dictionary;

        public Variables()
        {
            string variables_preloaded;

            variables_list = new List<Variable>();
            variables_dictionary = new Dictionary<string, Variable>();

            List<Variable> variables_preloaded_list;

            DebugBot.BlankLine();

            DebugBot.Notify("Loading Variables");
            DebugBot.PrintLine("File path:", file_path);

            variables_preloaded = File.ReadAllText(file_path);
            variables_preloaded_list = JsonConvert.DeserializeObject<List<Variable>>(variables_preloaded);

            if (variables_preloaded_list != null)
            {
                foreach (Variable variable in variables_preloaded_list)
                {
                    Load(variable);
                }
            }
        }

        #region Load variables

        /// <summary>
        /// Loads a <see cref="Variable"/> into the <see cref="variables_dictionary"/> and then <see cref="variables_list"/> to be used in real time.
        /// </summary>
        /// <param name="variable">The variable to load.</param>
        private void Load(Variable variable)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Loading variable...");

            if (!CheckSyntax(DebugMethod.LOAD, variable))
            {
                DebugBot.Error(DebugMethod.LOAD, nameof(variable), DebugError.NORMAL_SYNTAX);
                DebugBot.PrintObject(variable);

                return;
            }
            
            if (Exists(variable.key))
            {
                DebugBot.Error(DebugMethod.LOAD, nameof(variable), DebugError.NORMAL_EXISTS_YES);
                DebugBot.PrintLine(nameof(variable.key), variable.key);

                return;
            }

            try
            {
                variables_list.Add(variable);
                variables_dictionary.Add(variable.key, variable);

                DebugBot.Success(DebugMethod.LOAD, nameof(variable));
                DebugBot.PrintObject(variable);
            }
            catch (Exception exception)
            {
                DebugBot.Error(DebugMethod.LOAD, nameof(variable), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(variable.key), variable.key);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        #endregion

        #region Add, Edit, and Remove variables

        /// <summary>
        /// Modify the variables by adding, editting, or removing.
        /// </summary>
        /// <param name="commands">Used for parsing the body.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="TwitchNotify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        public void Modify(Commands commands, MessageTwitch message)
        {
            string temp = commands.ParseAfterCommand(message),
                   key = temp.TextBefore(" ");

            message.body = temp.TextAfter(" ");

            try
            {
                switch (key)
                {
                    case "!add":
                        Add(commands, message);
                        break;
                    case "!edit":
                        Edit(message);
                        break;
                    case "!remove":
                        Remove(message);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception exception)
            {
                DebugBot.Error(DebugMethod.MODIFY, "variable", DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(exception), exception.Message);
                DebugBot.PrintLine(nameof(temp), temp);
                DebugBot.PrintLine(nameof(key), key);
                DebugBot.PrintLine(nameof(message.body), message.body);
            }
        }

        private void Add(Commands commands, MessageTwitch message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Adding variable...");

            Variable variable = MessageToVariable(message);

            if(variable != default(Variable))
            {               
                Add(variable, message);
            }            
        }

        /// <summary>
        /// Adds a variable with a given value into the <see cref="variables_dictionary"/> in real time.
        /// </summary>
        /// <param name="variable">Variable to be added</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="TwitchNotify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        public void Add(Variable variable, MessageTwitch message)
        {           
            if (variable == default(Variable))
            {
                TwitchNotify.Error(DebugMethod.ADD, message, nameof(variable), variable.key, DebugError.NORMAL_SERIALIZE);

                DebugBot.Error(DebugMethod.ADD, nameof(variable), DebugError.NORMAL_SERIALIZE);

                return;
            }

            if (!CheckSyntax(DebugMethod.ADD, variable))
            {
                TwitchNotify.Error(DebugMethod.ADD, message, nameof(variable), variable.key, DebugError.NORMAL_NULL);

                DebugBot.Error(DebugMethod.ADD, nameof(variable), DebugError.NORMAL_SYNTAX);
                DebugBot.PrintObject(variable);

                return;
            }

            if (Exists(variable.key))
            {
                TwitchNotify.Error(DebugMethod.ADD, message, nameof(variable), variable.key, DebugError.NORMAL_EXISTS_YES);

                DebugBot.Error(DebugMethod.ADD, nameof(variable), DebugError.NORMAL_EXISTS_YES);
                DebugBot.PrintLine(nameof(variable.key), variable.key);

                return;
            }

            try
            {
                variables_list.Add(variable);
                variables_dictionary.Add(variable.key, variable);

                JsonConvert.SerializeObject(variables_list, Formatting.Indented).OverrideFile(file_path);

                TwitchNotify.Success(DebugMethod.ADD, message, nameof(variable), variable.key);

                DebugBot.Success(DebugMethod.ADD, nameof(variable));
                DebugBot.PrintObject(variable);
            }
            catch (Exception exception)
            {
                TwitchNotify.Error(DebugMethod.ADD, message, nameof(variable), variable.key, DebugError.NORMAL_EXCEPTION);

                DebugBot.Error(DebugMethod.ADD, nameof(variable), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(variable.key), variable.key);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Edits the value of a given variable in the <see cref="variables_dictionary"/> in real time.
        /// </summary>
        /// <param name="variable">Variable key to be edited.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="TwitchNotify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        private void Edit(MessageTwitch message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Editing variable...");

            Variable variable = MessageToVariable(message);

            if (variable == default(Variable))
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, nameof(variable), variable.key, DebugError.NORMAL_SERIALIZE);

                DebugBot.Error(DebugMethod.EDIT, nameof(variable), DebugError.NORMAL_SERIALIZE);

                return;
            }

            //check to see if the variable and value have the correct syntax
            if (!CheckSyntax(DebugMethod.EDIT, variable))
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, nameof(variable), variable.key, DebugError.NORMAL_SYNTAX);

                DebugBot.Error(DebugMethod.EDIT, nameof(variable), DebugError.NORMAL_SYNTAX);
                DebugBot.PrintObject(variable);

                return;
            }

            //make sure the variable exists
            if (!Exists(variable.key))
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, nameof(variable), variable.key, DebugError.NORMAL_EXISTS_NO);

                DebugBot.Error(DebugMethod.EDIT, nameof(variable), DebugError.NORMAL_EXISTS_NO);
                DebugBot.PrintLine(nameof(variable.key), variable.key);

                return;
            }

            try
            {
                variables_list.Remove(variables_dictionary[variable.key]);
                variables_list.Add(variable);

                variables_dictionary[variable.key] = variable;

                JsonConvert.SerializeObject(variables_list, Formatting.Indented).OverrideFile(file_path);

                TwitchNotify.Success(DebugMethod.EDIT, message, nameof(variable), variable.key);

                DebugBot.Success(DebugMethod.EDIT, nameof(variable));
                DebugBot.PrintObject(variable);
            }
            catch (Exception exception)
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, nameof(variable), variable.key, DebugError.NORMAL_EXCEPTION);

                DebugBot.Error(DebugMethod.EDIT, nameof(variable), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(variable.key), variable.key);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Removed the specified variable from the <see cref="variables_dictionary"/> in real time.
        /// </summary>
        /// <param name="variable">Variable key to be removed.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="TwitchNotify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        private void Remove(MessageTwitch message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Removing variable...");

            Variable variable = MessageToVariable(message);

            if (variable == default(Variable))
            {
                TwitchNotify.Error(DebugMethod.REMOVE, message, nameof(variable), variable.key, DebugError.NORMAL_SERIALIZE);

                DebugBot.Error(DebugMethod.REMOVE, nameof(variable), DebugError.NORMAL_SERIALIZE);

                return;
            }

            if (!Exists(variable.key))
            {
                TwitchNotify.Error(DebugMethod.REMOVE, message, nameof(variable), variable.key, DebugError.NORMAL_EXISTS_NO);

                DebugBot.Error(DebugMethod.REMOVE, nameof(variable), DebugError.NORMAL_EXISTS_NO);
                DebugBot.PrintLine(nameof(variable.key), variable.key);

                return;
            }

            try
            {
                variables_list.Remove(variables_dictionary[variable.key]);
                variables_dictionary.Remove(variable.key);

                JsonConvert.SerializeObject(variables_list, Formatting.Indented).OverrideFile(file_path);

                TwitchNotify.Success(DebugMethod.REMOVE, message, nameof(variable), variable.key);

                DebugBot.Success(DebugMethod.REMOVE, nameof(variable));
                DebugBot.PrintObject(variable);
            }
            catch (Exception exception)
            {
                TwitchNotify.Error(DebugMethod.REMOVE, message, nameof(variable), variable.key, DebugError.NORMAL_EXCEPTION);

                DebugBot.Error(DebugMethod.REMOVE, nameof(variable), DebugError.NORMAL_EXCEPTION);
                DebugBot.PrintLine(nameof(variable.key), variable.key);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }
        }

        #endregion

        #region Boolean checks

        /// <summary>
        /// Checks to see if a variable already exists.
        /// </summary>
        /// <param name="variable">Variable key to check.</param>
        /// <returns></returns>
        private bool Exists(string variable)
        {
            return variables_dictionary.ContainsKey(variable);
        }

        /// <summary>
        /// Checks to see if the variable and value match the proper syntax.
        /// </summary>
        /// <param name="variable">Variable to be checked for proper syntax.</param>
        /// <returns></returns>
        private bool CheckSyntax(DebugMethod method, Variable variable)
        {
            //check to see if the strings are null
            if (!variable.key.CheckString())
            {
                DebugBot.Error(method, nameof(variable), DebugError.SYNTAX_NULL);
                DebugBot.PrintLine(nameof(variable.key), "null");

                return false;
            }

            if (!variable.value.CheckString())
            {
                DebugBot.Error(method, nameof(variable), DebugError.SYNTAX_NULL);
                DebugBot.PrintLine(nameof(variable.key), variable.key);
                DebugBot.PrintLine(nameof(variable.value), "null");

                return false;
            }

            //check to see if the key is wrapped in the indicators
            if (!variable.key.StartsWith(lower_variable_indicator.ToString()) || !variable.key.EndsWith(upper_variable_indicator.ToString()))
            {
                DebugBot.Error(method, nameof(variable), DebugError.SYNTAX_SQUARE_BRACKETS_ENCLOSED_YES);
                DebugBot.PrintLine(nameof(variable.key), variable.key);

                return false;
            }                                   

            string _key = variable.key.Substring(1, variable.key.Length - 2);

            //check for illegal characters in the key
            if (_key.Contains("{") || _key.Contains("}"))
            {
                DebugBot.Error(method, nameof(variable), DebugError.SYNTAX_BRACKETS_NO);
                DebugBot.PrintLine(nameof(variable.key), variable.key);

                return false;
            }

            if (_key.Contains(lower_variable_indicator.ToString()) || _key.Contains(upper_variable_indicator.ToString()))
            {
                DebugBot.Error(method, nameof(variable), DebugError.SYNTAX_SQUARE_BRACKETS_NO);
                DebugBot.PrintLine(nameof(variable.key), _key);

                return false;
            }

            if (_key.Contains(lower_variable_search.ToString()) || _key.Contains(upper_variable_search.ToString()))
            {
                DebugBot.Error(method, nameof(variable), DebugError.SYNTAX_PARENTHESIS_NO);
                DebugBot.PrintLine(nameof(variable.key), _key);

                return false;
            }

            if (_key.Contains(" "))
            {
                DebugBot.Error(method, nameof(variable), DebugError.SYNTAX_SPACES_NO);
                DebugBot.PrintLine(nameof(variable.key), variable.key);

                return false;
            }

            //check for illegal characters in the value
            if (variable.value.Contains("{") || variable.value.Contains("}"))
            {
                DebugBot.Error(method, nameof(variable), DebugError.SYNTAX_BRACKETS_NO);
                DebugBot.PrintLine(nameof(variable.value), variable.value);

                return false;
            }

            if (variable.value.Contains(lower_variable_indicator.ToString()) || variable.value.Contains(upper_variable_indicator.ToString()))
            {
                DebugBot.Error(method, nameof(variable), DebugError.SYNTAX_SQUARE_BRACKETS_NO);
                DebugBot.PrintLine(nameof(variable.value), variable.value);

                return false;
            }

            if (variable.value.Contains(lower_variable_search.ToString()) || variable.value.Contains(upper_variable_search.ToString()))
            {
                DebugBot.Error(method, nameof(variable), DebugError.SYNTAX_PARENTHESIS_NO);
                DebugBot.PrintLine(nameof(variable.value), variable.value);

                return false;
            }            

            return true;
        }

        #endregion

        #region String parsing        

        /// <summary>
        /// Converts a message recieved from Twitch into a <see cref="Variable"/> and returns the variable.
        /// Returns default <see cref="Variable"/> if the message could not be converted.
        /// </summary>
        /// <param name="method">The type of operation being performed.</param>
        /// <param name="commands">Parses the body of a <see cref="MessageTwitch"/> after the command and returns a <see cref="string"/> to be processed as a <see cref="Variable"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="TwitchNotify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <returns></returns>
        private Variable MessageToVariable(MessageTwitch message)
        {
            string variable_string = message.body;

            variable_string = variable_string.PreserializeAs<string>();

            try
            {
                Variable variable = JsonConvert.DeserializeObject<Variable>(variable_string);

                DebugBot.Success(DebugMethod.SERIALIZE, nameof(variable_string));
                DebugBot.PrintObject(variable);

                return variable;
            }
            catch (Exception exception)
            {
                DebugBot.Error(DebugMethod.SERIALIZE, nameof(variable_string), DebugError.NORMAL_EXCEPTION);                
                DebugBot.PrintLine(nameof(variable_string), variable_string);
                DebugBot.PrintLine(nameof(exception), exception.Message);

                return default(Variable);
            }
        }

        /// <summary>
        /// Converts a custom string into a <see cref="Variable"/> and returns the variable.
        /// Called from <see cref="ExtractVariables(string, MessageTwitch, out Variable[])"/>.
        /// Returns default <see cref="Variable"/> if the message could not be converted.
        /// </summary>
        /// <param name="method">The type of operation being performed.</param>
        /// <param name="variable_string">The string to be converted and serialized into a variable</param>
        /// <returns></returns>
        private Variable MessageToVariable(DebugMethod method, MessageTwitch message, string variable_string)
        {
            variable_string = variable_string.PreserializeAs<string>();

            try
            {
                Variable variable = JsonConvert.DeserializeObject<Variable>(variable_string);

                DebugBot.Success(DebugMethod.SERIALIZE, nameof(variable_string));
                DebugBot.PrintObject(variable);

                return variable;
            }
            catch (Exception exception)
            {
                TwitchNotify.Error(method, message, nameof(variable_string), variable_string, DebugError.NORMAL_EXCEPTION);

                DebugBot.Error(DebugMethod.SERIALIZE, nameof(variable_string), DebugError.NORMAL_EXCEPTION);
                DebugBot.Error(method, nameof(variable_string), DebugError.NORMAL_EXCEPTION);

                DebugBot.PrintLine(nameof(variable_string), variable_string);
                DebugBot.PrintLine(nameof(exception), exception.Message);

                return default(Variable);
            }
        }

        /// <summary>
        /// Loops through the body of the <see cref="MessageTwitch"/> and attempts to add any variables found.
        /// Returns the new message body with the successfully extracted varibale keys.
        /// </summary>
        /// <param name="response">The body of the <see cref="MessageTwitch"/> to be parsed for variables to be extracted.</param>
        /// <param name="variable_array">An array off all the extracted and serialized <see cref="Variable"/>.</param>
        /// <returns></returns>
        public string ExtractVariables(string response, MessageTwitch message, out Variable[] variable_array)
        {
            string extracted_variable = response;

            Variable variable;

            List<Variable> list = new List<Variable>();

            int index = 1;

            do
            {
                extracted_variable = response.TextBetween(lower_variable_search, upper_variable_search, StringSearch.Occurrence, index);

                if (!extracted_variable.CheckString())
                {
                    continue;
                }

                try
                {                    
                    variable = MessageToVariable(DebugMethod.ADD, message, extracted_variable);
                    response = response.Replace(lower_variable_search + extracted_variable + upper_variable_search, variable.key);

                    list.Add(variable);
                }
                catch(Exception exception)
                {
                    DebugBot.Error(DebugMethod.ADD, nameof(variable), DebugError.NORMAL_EXCEPTION);
                    DebugBot.PrintLine(nameof(extracted_variable), extracted_variable);
                    DebugBot.PrintLine(nameof(exception), exception.Message);
                }

                ++index;
            }
            while (extracted_variable.CheckString());

            variable_array = list.ToArray();

            return response;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets the dictionary of the loaded variables
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, Variable> GetVariables()
        {
            return variables_dictionary;
        }

        #endregion
    }
}
