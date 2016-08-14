using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

using TwitchChatBot.Debugger;
using TwitchChatBot.Enums.Debug;
using TwitchChatBot.Enums.Extensions;
using TwitchChatBot.Extensions;
using TwitchChatBot.Extensions.Files;
using TwitchChatBot.Models.Bot;

namespace TwitchChatBot.Chat
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

            BotDebug.BlankLine();

            BotDebug.BlockBegin();
            BotDebug.Header("Loading Variables");
            BotDebug.PrintLine("File path:", file_path);

            variables_preloaded = File.ReadAllText(file_path);
            variables_preloaded_list = JsonConvert.DeserializeObject<List<Variable>>(variables_preloaded);

            if (variables_preloaded_list != null)
            {
                foreach (Variable variable in variables_preloaded_list)
                {
                    Load(variable);
                }
            }

            BotDebug.BlockEnd();
        }

        #region Load variables

        /// <summary>
        /// Loads a <see cref="Variable"/> into the <see cref="variables_dictionary"/> and then <see cref="variables_list"/> to be used in real time.
        /// </summary>
        /// <param name="variable">The variable to load.</param>
        private void Load(Variable variable)
        {
            BotDebug.BlankLine();
            BotDebug.SubHeader("Loading variable...");

            if (!CheckSyntax(variable))
            {
                BotDebug.Error(DebugMethod.Load, DebugObject.Variable, DebugError.Syntax);
                BotDebug.PrintObject(variable);

                return;
            }

            if (Exists(variable.key))
            {
                BotDebug.Error(DebugMethod.Load, DebugObject.Variable, DebugError.ExistYes);
                BotDebug.PrintLine(nameof(variable.key), variable.key);

                return;
            }

            try
            {
                variables_list.Add(variable);
                variables_dictionary.Add(variable.key, variable);

                BotDebug.Success(DebugMethod.Load, DebugObject.Variable, variable.key);
                BotDebug.PrintObject(variable);
            }
            catch (Exception exception)
            {
                BotDebug.Error(DebugMethod.Load, DebugObject.Variable, DebugError.Exception);
                BotDebug.PrintLine(nameof(variable.key), variable.key);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }
        }

        #endregion

        #region Add, Edit, and Remove variables

        /// <summary>
        /// Modify the variables by adding, editting, or removing.
        /// </summary>
        /// <param name="commands">Used for parsing the body.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        public void Modify(Commands commands, Message message)
        {
            string temp = commands.ParseCommandString(message),
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
                BotDebug.Error(DebugMethod.Modify, DebugObject.Variable, DebugError.Exception);
                BotDebug.PrintLine(nameof(exception), exception.Message);
                BotDebug.PrintLine(nameof(temp), temp);
                BotDebug.PrintLine(nameof(key), key);
                BotDebug.PrintLine(nameof(message.body), message.body);
            }
        }

        private void Add(Commands commands, Message message)
        {
            BotDebug.BlankLine();
            BotDebug.SubHeader("Adding variable...");

            Variable variable = MessageToVariable(DebugMethod.Add, message);

            if(variable != default(Variable))
            {
                Add(variable, message);
            }            
        }

        /// <summary>
        /// Adds a variable with a given value into the <see cref="variables_dictionary"/> in real time.
        /// </summary>
        /// <param name="variable">Variable to be added</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        public void Add(Variable variable, Message message)
        {           
            if (variable == default(Variable))
            {
                return;
            }

            if (!CheckSyntax(variable))
            {
                Notify.Error(DebugMethod.Add, DebugObject.Variable, variable.key, DebugError.Syntax, message);

                BotDebug.Error(DebugMethod.Add, DebugObject.Variable, DebugError.Syntax);
                BotDebug.PrintObject(variable);

                return;
            }

            if (Exists(variable.key))
            {
                Notify.Error(DebugMethod.Add, DebugObject.Variable, variable.key, DebugError.ExistYes, message);

                BotDebug.Error(DebugMethod.Add, DebugObject.Variable, DebugError.ExistYes);
                BotDebug.PrintLine(nameof(variable.key), variable.key);

                return;
            }

            try
            {
                variables_list.Add(variable);
                variables_dictionary.Add(variable.key, variable);

                JsonConvert.SerializeObject(variables_list, Formatting.Indented).OverrideFile(file_path);

                Notify.Success(DebugMethod.Add, DebugObject.Variable, variable.key, message);

                BotDebug.Success(DebugMethod.Add, DebugObject.Variable, variable.key);
                BotDebug.PrintObject(variable);
            }
            catch (Exception exception)
            {
                Notify.Error(DebugMethod.Add, DebugObject.Variable, variable.key, DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Add, DebugObject.Variable, DebugError.Exception);
                BotDebug.PrintLine(nameof(variable.key), variable.key);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Edits the value of a given variable in the <see cref="variables_dictionary"/> in real time.
        /// </summary>
        /// <param name="variable">Variable key to be edited.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        private void Edit(Message message)
        {
            BotDebug.BlankLine();
            BotDebug.SubHeader("Editing variable...");

            Variable variable = MessageToVariable(DebugMethod.Edit, message);

            if (variable == default(Variable))
            {
                return;
            }

            //check to see if the variable and value have the correct syntax
            if (!CheckSyntax(variable))
            {
                Notify.Error(DebugMethod.Edit, DebugObject.Variable, variable.key, DebugError.Syntax, message);

                BotDebug.Error(DebugMethod.Edit, DebugObject.Variable, DebugError.Syntax);
                BotDebug.PrintObject(variable);

                return;
            }

            //make sure the variable exists
            if (!Exists(variable.key))
            {
                Notify.Error(DebugMethod.Edit, DebugObject.Variable, variable.key, DebugError.ExistNo, message);

                BotDebug.Error(DebugMethod.Edit, DebugObject.Variable, DebugError.ExistNo);
                BotDebug.PrintLine(nameof(variable.key), variable.key);

                return;
            }

            try
            {
                variables_list.Remove(variables_dictionary[variable.key]);
                variables_list.Add(variable);

                variables_dictionary[variable.key] = variable;

                JsonConvert.SerializeObject(variables_list, Formatting.Indented).OverrideFile(file_path);

                Notify.Success(DebugMethod.Edit, DebugObject.Variable, variable.key, message);

                BotDebug.Success(DebugMethod.Edit, DebugObject.Variable, variable.key);
                BotDebug.PrintObject(variable);
            }
            catch (Exception exception)
            {
                Notify.Error(DebugMethod.Edit, DebugObject.Variable, variable.key, DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Edit, DebugObject.Variable, DebugError.Exception);
                BotDebug.PrintLine(nameof(variable.key), variable.key);
                BotDebug.PrintLine(nameof(exception), exception.Message);
            }
        }

        /// <summary>
        /// Removed the specified variable from the <see cref="variables_dictionary"/> in real time.
        /// </summary>
        /// <param name="variable">Variable key to be removed.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        private void Remove(Message message)
        {
            BotDebug.BlankLine();
            BotDebug.SubHeader("Removing variable...");

            Variable variable = MessageToVariable(DebugMethod.Remove, message);

            if (variable == default(Variable))
            {
                return;
            }

            if (!Exists(variable.key))
            {
                Notify.Error(DebugMethod.Remove, DebugObject.Variable, variable.key, DebugError.ExistNo, message);

                BotDebug.Error(DebugMethod.Remove, DebugObject.Variable, DebugError.ExistNo);
                BotDebug.PrintLine(nameof(variable.key), variable.key);

                return;
            }

            try
            {
                variables_list.Remove(variables_dictionary[variable.key]);
                variables_dictionary.Remove(variable.key);

                JsonConvert.SerializeObject(variables_list, Formatting.Indented).OverrideFile(file_path);

                Notify.Success(DebugMethod.Remove, DebugObject.Variable, variable.key, message);

                BotDebug.Success(DebugMethod.Remove, DebugObject.Variable, variable.key);
                BotDebug.PrintObject(variable);
            }
            catch (Exception exception)
            {
                Notify.Error(DebugMethod.Remove, DebugObject.Variable, variable.key, DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Remove, DebugObject.Variable, DebugError.Exception);
                BotDebug.PrintLine(nameof(variable.key), variable.key);
                BotDebug.PrintLine(nameof(exception), exception.Message);
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
        private bool CheckSyntax(Variable variable)
        {
            //check to see if the strings are null
            if (!variable.key.CheckString())
            {
                BotDebug.SyntaxError(DebugObject.Variable, DebugObject.Variable, SyntaxError.Null);
                BotDebug.PrintLine(nameof(variable.key), "null");

                return false;
            }

            if (!variable.value.CheckString())
            {
                BotDebug.SyntaxError(DebugObject.Variable, DebugObject.Value, SyntaxError.Null);
                BotDebug.PrintObject(variable);

                return false;
            }

            //check to see if the key is wrapped in the indicators
            if (!variable.key.StartsWith(lower_variable_indicator.ToString()) || !variable.key.EndsWith(upper_variable_indicator.ToString()))
            {
                BotDebug.SyntaxError(DebugObject.Variable, DebugObject.Variable, SyntaxError.SquareBracketsYes);
                BotDebug.PrintLine(nameof(variable.key), variable.key);

                return false;
            }                                   

            string _variable = variable.key.Substring(1, variable.key.Length - 2);

            //check for illegal characters in the key
            if (_variable.Contains("{") || _variable.Contains("}"))
            {
                BotDebug.SyntaxError(DebugObject.Variable, DebugObject.Variable, SyntaxError.BracketsNo);
                BotDebug.PrintLine(nameof(variable.key), variable.key);

                return false;
            }

            if (_variable.Contains(lower_variable_indicator.ToString()) || _variable.Contains(upper_variable_indicator.ToString()))
            {
                BotDebug.SyntaxError(DebugObject.Variable, DebugObject.Variable, SyntaxError.SquareBracketsNo);
                BotDebug.PrintLine(nameof(variable.key), variable.key);

                return false;
            }

            if (_variable.Contains(lower_variable_search.ToString()) || _variable.Contains(upper_variable_search.ToString()))
            {
                BotDebug.SyntaxError(DebugObject.Variable, DebugObject.Variable, SyntaxError.ParenthesisNo);
                BotDebug.PrintLine(nameof(variable.key), variable.key);

                return false;
            }

            if (_variable.Contains(" "))
            {
                BotDebug.SyntaxError(DebugObject.Variable, DebugObject.Value, SyntaxError.Spaces);
                BotDebug.PrintLine(nameof(variable.key), variable.key);

                return false;
            }

            //check for illegal characters in the value
            if (variable.value.Contains("{") || variable.value.Contains("}"))
            {
                BotDebug.SyntaxError(DebugObject.Variable, DebugObject.Value, SyntaxError.BracketsNo);
                BotDebug.PrintObject(variable);

                return false;
            }

            if (variable.value.Contains(lower_variable_indicator.ToString()) || variable.value.Contains(upper_variable_indicator.ToString()))
            {
                BotDebug.SyntaxError(DebugObject.Variable, DebugObject.Value, SyntaxError.SquareBracketsNo);
                BotDebug.PrintObject(variable);

                return false;
            }

            if (variable.value.Contains(lower_variable_search.ToString()) || variable.value.Contains(upper_variable_search.ToString()))
            {
                BotDebug.SyntaxError(DebugObject.Variable, DebugObject.Value, SyntaxError.ParenthesisNo);
                BotDebug.PrintObject(variable);

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
        /// <param name="commands">Parses the body of a <see cref="Message"/> after the command and returns a <see cref="string"/> to be processed as a <see cref="Variable"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <returns></returns>
        private Variable MessageToVariable(DebugMethod method, Message message)
        {
            string variable_string = message.body;

            variable_string = variable_string.PreserializeAs<string>();

            try
            {
                Variable variable = JsonConvert.DeserializeObject<Variable>(variable_string);

                BotDebug.Success(DebugMethod.Serialize, DebugObject.Command, variable.key);
                BotDebug.PrintObject(variable);

                return variable;
            }
            catch (Exception exception)
            {
                Notify.Error(method, DebugObject.Variable, variable_string, DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Serialize, DebugObject.Variable, DebugError.Exception);
                BotDebug.Error(method, DebugObject.Variable, DebugError.Null);
                BotDebug.PrintLine(nameof(variable_string), variable_string);
                BotDebug.PrintLine(nameof(exception), exception.Message);

                return default(Variable);
            }
        }

        /// <summary>
        /// Converts a custom string into a <see cref="Variable"/> and returns the variable.
        /// Called from <see cref="ExtractVariables(string, Message, out Variable[])"/>.
        /// Returns default <see cref="Variable"/> if the message could not be converted.
        /// </summary>
        /// <param name="method">The type of operation being performed.</param>
        /// <param name="variable_string">The string to be converted and serialized into a variable</param>
        /// <returns></returns>
        private Variable MessageToVariable(DebugMethod method, Message message, string variable_string)
        {
            variable_string = variable_string.PreserializeAs<string>();

            try
            {
                Variable variable = JsonConvert.DeserializeObject<Variable>(variable_string);

                BotDebug.Success(DebugMethod.Serialize, DebugObject.Command, variable.key);
                BotDebug.PrintObject(variable);

                return variable;
            }
            catch (Exception exception)
            {
                Notify.Error(method, DebugObject.Variable, variable_string, DebugError.Exception, message);

                BotDebug.Error(DebugMethod.Serialize, DebugObject.Variable, DebugError.Exception);
                BotDebug.Error(method, DebugObject.Variable, DebugError.Null);
                BotDebug.PrintLine(nameof(variable_string), variable_string);
                BotDebug.PrintLine(nameof(exception), exception.Message);

                return default(Variable);
            }
        }

        /// <summary>
        /// Loops through the body of the <see cref="Message"/> and attempts to add any variables found.
        /// Returns the new message body with the successfully extracted varibale keys.
        /// </summary>
        /// <param name="response">The body of the <see cref="Message"/> to be parsed for variables to be extracted.</param>
        /// <param name="variable_array">An array off all the extracted and serialized <see cref="Variable"/>.</param>
        /// <returns></returns>
        public string ExtractVariables(string response, Message message, out Variable[] variable_array)
        {
            string extracted_variable = string.Empty;

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
                    variable = MessageToVariable(DebugMethod.Add, message, extracted_variable);
                    response = response.Replace(lower_variable_search + extracted_variable + upper_variable_search, variable.key);

                    list.Add(variable);
                }
                catch(Exception exception)
                {
                    BotDebug.Error(DebugMethod.Add, DebugObject.Variable, DebugError.Exception);
                    BotDebug.PrintLine(nameof(extracted_variable), extracted_variable);
                    BotDebug.PrintLine(nameof(exception), exception.Message);
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
