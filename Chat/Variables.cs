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
        //indicates the bounds to parse between to find a variable 
        char lower_variable_indicator = '[',
             upper_variable_indicator = ']',
             lower_variable_search = '(',
             upper_variable_search = ')';

        string file_path = Environment.CurrentDirectory + "/JSON/Chat/Variables.json";

        List<Variable> variables_list = new List<Variable>();

        Dictionary<string, Variable> variables_dictionary = new Dictionary<string, Variable>();

        public Variables()
        {
            string variables_preloaded;

            List<Variable> variables_preloaded_list;

            Debug.BlankLine();

            Debug.BlockBegin();
            Debug.Header("Loading Variables");
            Debug.PrintLine("File path:", file_path);

            variables_preloaded = File.ReadAllText(file_path);
            variables_preloaded_list = JsonConvert.DeserializeObject<List<Variable>>(variables_preloaded);

            if (variables_preloaded_list != null)
            {
                foreach (Variable variable in variables_preloaded_list)
                {
                    Load(variable);
                }
            }

            Debug.BlockEnd();
        }

        #region Load variables

        /// <summary>
        /// Loads a <see cref="Variable"/> into the <see cref="variables_dictionary"/> and then <see cref="variables_list"/> to be used in real time.
        /// </summary>
        /// <param name="variable">The variable to load.</param>
        private void Load(Variable variable)
        {
            Debug.BlankLine();
            Debug.SubHeader("Loading variable...");

            if (!CheckSyntax(variable.key, variable.value))
            {
                Debug.Error(DebugMethod.Load, DebugObject.Variable, DebugError.Syntax);
                Debug.PrintObject(variable);

                return;
            }

            if (Exists(variable.key))
            {
                Debug.Error(DebugMethod.Load, DebugObject.Variable, DebugError.ExistYes);
                Debug.PrintLine("key:", variable.key);

                return;
            }

            try
            {
                variables_list.Add(variable);
                variables_dictionary.Add(variable.key, variable);

                Debug.Success(DebugMethod.Load, DebugObject.Variable, variable.key);
                Debug.PrintObject(variable);
            }
            catch (Exception ex)
            {
                Debug.Error(DebugMethod.Load, DebugObject.Variable, DebugError.Exception);
                Debug.PrintLine("key:", variable.key);
                Debug.PrintLine("Exception:", ex.Message);

                return;
            }
        }

        #endregion

        #region Add, Edit, and Remove variables

        /// <summary>
        /// Parses the body of a <see cref="Message"/> for a command and attempts to add the specified command.
        /// Called from Twitch by using <code>!addvariable</code>.
        /// </summary>
        /// <param name="commands">Parses the body of a <see cref="Message"/> after the command and returns a <see cref="string"/> to be processed as a <see cref="Variable"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        public void Add(Commands commands, Message message)
        {
            Debug.BlankLine();
            Debug.SubHeader("Adding variable...");

            Variable variable = MessageToVariable(DebugMethod.Add, commands, message);

            if (variable == null)
            {
                return;
            }

            Add(variable, message);
        }

        /// <summary>
        /// Adds a variable with a given value into the <see cref="variables_dictionary"/> in real time.
        /// </summary>
        /// <param name="variable">Variable to be added</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        public void Add(Variable variable, Message message)
        {                                  
            if (!CheckSyntax(variable.key, variable.value))
            {
                Notify.Failed(DebugMethod.Add, DebugObject.Variable, variable.key, DebugError.Syntax, message);

                Debug.Error(DebugMethod.Add, DebugObject.Variable, DebugError.Syntax);
                Debug.PrintObject(variable);

                return;
            }

            if (Exists(variable.key))
            {
                Notify.Failed(DebugMethod.Add, DebugObject.Variable, variable.key, DebugError.ExistYes, message);

                Debug.Error(DebugMethod.Add, DebugObject.Variable, DebugError.ExistYes);
                Debug.PrintLine("key:", variable.key);

                return;
            }

            try
            {
                variables_list.Add(variable);
                variables_dictionary.Add(variable.key, variable);

                JsonConvert.SerializeObject(variables_list, Formatting.Indented).OverrideFile(file_path);

                Notify.Success(DebugMethod.Add, DebugObject.Variable, variable.key, message);

                Debug.Success(DebugMethod.Add, DebugObject.Variable, variable.key);
                Debug.PrintObject(variable);
            }
            catch (Exception ex)
            {
                Notify.Failed(DebugMethod.Add, DebugObject.Variable, variable.key, DebugError.Exception, message);

                Debug.Error(DebugMethod.Add, DebugObject.Variable, DebugError.Exception);
                Debug.PrintLine("key:", variable.key);
                Debug.PrintLine("Exception:", ex.Message);

                return;
            }
        }

        /// <summary>
        /// Parses the body of a <see cref="Message"/> for a command and attempts to edit the specified command.
        /// Called from Twitch by using <code>!editvariable</code>.
        /// </summary>
        /// <param name="commands">Parses the body of a <see cref="Message"/> after the command and returns a <see cref="string"/> to be processed as a <see cref="Variable"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        public void Edit(Commands commands, Message message)
        {
            Debug.BlankLine();
            Debug.SubHeader("Editing variable...");

            Variable variable = MessageToVariable(DebugMethod.Edit, commands, message);

            if (variable == null)
            {
                return;
            }

            Edit(variable, message);
        }

        /// <summary>
        /// Edits the value of a given variable in the <see cref="variables_dictionary"/> in real time.
        /// </summary>
        /// <param name="variable">Variable key to be edited.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        private void Edit(Variable variable, Message message)
        {
            //check to see if the variable and value have the correct syntax
            if (!CheckSyntax(variable.key, variable.value))
            {
                Notify.Failed(DebugMethod.Edit, DebugObject.Variable, variable.key, DebugError.Syntax, message);

                Debug.Error(DebugMethod.Edit, DebugObject.Variable, DebugError.Syntax);
                Debug.PrintObject(variable);

                return;
            }

            //make sure the variable exists
            if (!Exists(variable.key))
            {
                Notify.Failed(DebugMethod.Edit, DebugObject.Variable, variable.key, DebugError.ExistNo, message);

                Debug.Error(DebugMethod.Edit, DebugObject.Variable, DebugError.ExistNo);
                Debug.PrintLine("key:", variable.key);

                return;
            }

            try
            {
                variables_list.Remove(variables_dictionary[variable.key]);
                variables_list.Add(variable);

                variables_dictionary[variable.key] = variable;

                JsonConvert.SerializeObject(variables_list, Formatting.Indented).OverrideFile(file_path);

                Notify.Success(DebugMethod.Edit, DebugObject.Variable, variable.key, message);

                Debug.Success(DebugMethod.Edit, DebugObject.Variable, variable.key);
                Debug.PrintObject(variable);
            }
            catch (Exception ex)
            {
                Notify.Failed(DebugMethod.Edit, DebugObject.Variable, variable.key, DebugError.Exception, message);

                Debug.Error(DebugMethod.Edit, DebugObject.Variable, DebugError.Exception);
                Debug.PrintLine("key:", variable.key);
                Debug.PrintLine("Exception: ", ex.Message);
            }
        }

        /// <summary>
        /// Parses the body of a <see cref="Message"/> for a variable and attempts to remove the specified variable.
        /// Called from Twitch by using <code>!removevariable</code>.
        /// </summary>
        /// <param name="commands">Parses the body of a <see cref="Message"/> after the command and returns a <see cref="string"/> to be processed as a <see cref="Variable"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        public void Remove(Commands commands, Message message)
        {
            Debug.BlankLine();
            Debug.SubHeader("Removing variable...");

            Variable variable = MessageToVariable(DebugMethod.Remove, commands, message);

            if (variable == null)
            {
                return;
            }

            Remove(variable, message);
        }

        /// <summary>
        /// Removed the specified variable from the <see cref="variables_dictionary"/> in real time.
        /// </summary>
        /// <param name="variable">Variable key to be removed.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        private void Remove(Variable variable, Message message)
        {
            if (!Exists(variable.key))
            {
                Notify.Failed(DebugMethod.Remove, DebugObject.Variable, variable.key, DebugError.ExistNo, message);

                Debug.Error(DebugMethod.Remove, DebugObject.Variable, DebugError.ExistNo);
                Debug.PrintLine("key:", variable.key);

                return;
            }

            try
            {
                variables_list.Remove(variables_dictionary[variable.key]);
                variables_dictionary.Remove(variable.key);

                JsonConvert.SerializeObject(variables_list, Formatting.Indented).OverrideFile(file_path);

                Notify.Success(DebugMethod.Remove, DebugObject.Variable, variable.key, message);

                Debug.Success(DebugMethod.Remove, DebugObject.Variable, variable.key);
                Debug.PrintObject(variable);
            }
            catch (Exception ex)
            {
                //shit hit the fan
                Notify.Failed(DebugMethod.Remove, DebugObject.Variable, variable.key, DebugError.Exception, message);

                Debug.Error(DebugMethod.Remove, DebugObject.Variable, DebugError.Exception);
                Debug.PrintLine("key:", variable.key);
                Debug.PrintLine("Exception:", ex.Message);
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
        /// <param name="variable">Variable key to be checked.</param>
        /// <param name="value">Value to be checked.</param>
        /// <returns></returns>
        private bool CheckSyntax(string variable, string value)
        {
            //check to see if the strings are null
            if (!variable.CheckString())
            {
                Debug.SyntaxError(DebugObject.Variable, DebugObject.Variable, SyntaxError.Null);
                Debug.PrintLine("key:", "null");

                return false;
            }

            if (!value.CheckString())
            {
                Debug.SyntaxError(DebugObject.Variable, DebugObject.Value, SyntaxError.Null);
                Debug.PrintLine("key:", variable);
                Debug.PrintLine("value:", value);

                return false;
            }

            //check to see if the key is wrapped in the indicators
            if (!variable.StartsWith(lower_variable_indicator.ToString()) || !variable.EndsWith(upper_variable_indicator.ToString()))
            {
                Debug.SyntaxError(DebugObject.Variable, DebugObject.Variable, SyntaxError.SquareBracketsYes);
                Debug.PrintLine("key:", variable);

                return false;
            }                                   

            string _variable = variable.Substring(1, variable.Length - 2);

            //check for illegal characters in the key
            if (_variable.Contains("{") || _variable.Contains("}"))
            {
                Debug.SyntaxError(DebugObject.Variable, DebugObject.Variable, SyntaxError.BracketsNo);
                Debug.PrintLine("key:", variable);

                return false;
            }

            if (_variable.Contains(lower_variable_indicator.ToString()) || _variable.Contains(upper_variable_indicator.ToString()))
            {
                Debug.SyntaxError(DebugObject.Variable, DebugObject.Variable, SyntaxError.SquareBracketsNo);
                Debug.PrintLine("key:", variable);

                return false;
            }

            if (_variable.Contains(lower_variable_search.ToString()) || _variable.Contains(upper_variable_search.ToString()))
            {
                Debug.SyntaxError(DebugObject.Variable, DebugObject.Variable, SyntaxError.ParenthesisNo);
                Debug.PrintLine("key:", variable);

                return false;
            }

            if (_variable.Contains(" "))
            {
                Debug.SyntaxError(DebugObject.Variable, DebugObject.Value, SyntaxError.Spaces);
                Debug.PrintLine("key:", variable);

                return false;
            }

            //check for illegal characters in the value
            if (value.Contains("{") || value.Contains("}"))
            {
                Debug.SyntaxError(DebugObject.Variable, DebugObject.Value, SyntaxError.BracketsNo);
                Debug.PrintLine("key:", variable);

                return false;
            }

            if (value.Contains(lower_variable_indicator.ToString()) || value.Contains(upper_variable_indicator.ToString()))
            {
                Debug.SyntaxError(DebugObject.Variable, DebugObject.Value, SyntaxError.SquareBracketsNo);
                Debug.PrintLine("key:", variable);
                Debug.PrintLine("value:", value);

                return false;
            }

            if (value.Contains(lower_variable_search.ToString()) || value.Contains(upper_variable_search.ToString()))
            {
                Debug.SyntaxError(DebugObject.Variable, DebugObject.Value, SyntaxError.ParenthesisNo);
                Debug.PrintLine("key:", variable);
                Debug.PrintLine("value:", value);

                return false;
            }            

            return true;
        }

        #endregion

        #region String parsing        

        /// <summary>
        /// Converts a message recieved from Twitch into a <see cref="Variable"/> and returns the variable.
        /// Returns null if the message could not be converted to a <see cref="Variable"/>.
        /// </summary>
        /// <param name="debug_method">The type of operation being performed.</param>
        /// <param name="commands">Parses the body of a <see cref="Message"/> after the command and returns a <see cref="string"/> to be processed as a <see cref="Variable"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <returns></returns>
        private Variable MessageToVariable(DebugMethod debug_method, Commands commands, Message message)
        {
            string variable_string;

            variable_string = commands.ParseCommandString(message);
            variable_string = variable_string.PreserializeAs<string>();

            try
            {
                Variable variable = JsonConvert.DeserializeObject<Variable>(variable_string);

                Debug.Success(DebugMethod.Serialize, DebugObject.Command, variable.key);
                Debug.PrintObject(variable);

                return variable;
            }
            catch (Exception ex)
            {
                Debug.Error(DebugMethod.Serialize, DebugObject.Variable, DebugError.Exception);
                Debug.Error(debug_method, DebugObject.Variable, DebugError.Null);
                Debug.PrintLine("key:", variable_string);
                Debug.PrintLine("Exception:", ex.Message);

                return null;
            }
        }

        /// <summary>
        /// Converts a custom string into a <see cref="Variable"/> and returns the variable.
        /// Returns null if the message could not be converted to a <see cref="Variable"/>.
        /// </summary>
        /// <param name="debug_method">The type of operation being performed.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <returns></returns>
        private Variable MessageToVariable(DebugMethod debug_method, string variable_string)
        {
            variable_string = variable_string.PreserializeAs<string>();

            try
            {
                Variable variable = JsonConvert.DeserializeObject<Variable>(variable_string);

                Debug.Success(DebugMethod.Serialize, DebugObject.Command, variable.key);
                Debug.PrintObject(variable);

                return variable;
            }
            catch (Exception ex)
            {
                Debug.Error(DebugMethod.Serialize, DebugObject.Variable, DebugError.Exception);
                Debug.Error(debug_method, DebugObject.Variable, DebugError.Null);
                Debug.PrintLine("key:", variable_string);
                Debug.PrintLine("Exception:", ex.Message);

                return null;
            }
        }

        /// <summary>
        /// Loops through the body of the <see cref="Message"/> and attempts to add any variables found.
        /// </summary>
        /// <param name="body">Body of the <see cref="Message"/> to be processed</param>
        /// <returns></returns>
        public string ExtractVariables(string response, Message message, out Variable[] variable_array)
        {
            string extracted_variable = "";

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
                    variable = MessageToVariable(DebugMethod.Add, extracted_variable);

                    response = response.Replace(lower_variable_search + extracted_variable + upper_variable_search, variable.key);

                    list.Add(variable);
                }
                catch(Exception ex)
                {
                    Debug.Error(DebugMethod.Add, DebugObject.Variable, DebugError.Exception);
                    Debug.PrintLine("key:", extracted_variable);
                    Debug.PrintLine("Exception:", ex.Message);
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
