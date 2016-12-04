using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

using TwitchBot.Debugger;
using TwitchBot.Enums.Debugger;
using TwitchBot.Enums.Extensions;
using TwitchBot.Extensions;
using TwitchBot.Extensions.Files;
using TwitchBot.Messages;
using TwitchBot.Models.Bot.Chat;

namespace TwitchBot.Chat
{
    class Variables
    {
        char lower_variable_indicator = '[',
             upper_variable_indicator = ']',
             lower_variable_search = '(',
             upper_variable_search = ')';

        readonly string FILE_PATH = Environment.CurrentDirectory + "/JSON/Chat/Variables.json";

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
            DebugBot.PrintLine("File path:", FILE_PATH);

            variables_preloaded = File.ReadAllText(FILE_PATH);
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
        /// Loads all the <see cref="Variable"/>s from the <see cref="FILE_PATH"/>.
        /// </summary>
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
        /// Modify commands by adding, editting, or removing commands.
        /// </summary>
        public void Modify(Commands commands, TwitchMessage message)
        {
            string temp = commands.ParseAfterCommand(message),
                   key = temp.TextBefore(" ");

            message.body = temp.TextAfter(" ");

            try
            {
                switch (key)
                {
                    case "!add":
                        Add(message);
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

        /// <summary>
        /// Adds a <see cref="Variable"/> at run time to be used in real time without needing to re-launch the bot.
        /// Wrapper for the underlying <see cref="Add(Variable, TwitchMessage)"/> method.
        /// </summary>
        private void Add(TwitchMessage message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Adding variable...");

            Variable variable = MessageToVariable(message);

            Add(variable, message);                        
        }

        /// <summary>
        /// Adds a <see cref="Variable"/> at run time to be used in real time without needing to re-launch the bot.
        /// Can be called externally to directly add a variable.
        /// </summary>                
        public void Add(Variable variable, TwitchMessage message)
        {
            //the varible was "empty" or could not be converted before hand, do nothing
            if (variable == default(Variable))
            {
                TwitchNotify.Error(DebugMethod.ADD, message, nameof(variable), variable.key, DebugError.NORMAL_DESERIALIZE);

                DebugBot.Error(DebugMethod.ADD, nameof(variable), DebugError.NORMAL_DESERIALIZE);

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

                JsonConvert.SerializeObject(variables_list, Formatting.Indented).OverrideFile(FILE_PATH);

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
        /// Edits a pre-existing <see cref="Variable"/> at run time without needing to re-launch the bot. 
        /// </summary>
        private void Edit(TwitchMessage message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Editing variable...");

            Variable variable = MessageToVariable(message);

            if (variable == default(Variable))
            {
                TwitchNotify.Error(DebugMethod.EDIT, message, nameof(variable), variable.key, DebugError.NORMAL_DESERIALIZE);

                DebugBot.Error(DebugMethod.EDIT, nameof(variable), DebugError.NORMAL_DESERIALIZE);

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

                JsonConvert.SerializeObject(variables_list, Formatting.Indented).OverrideFile(FILE_PATH);

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
        /// Removes a pre-existing <see cref="Variable"/> at run time without needing to re-launch the bot. 
        /// </summary>
        private void Remove(TwitchMessage message)
        {
            DebugBot.BlankLine();
            DebugBot.SubHeader("Removing variable...");

            Variable variable = MessageToVariable(message);

            if (variable == default(Variable))
            {
                TwitchNotify.Error(DebugMethod.REMOVE, message, nameof(variable), variable.key, DebugError.NORMAL_DESERIALIZE);

                DebugBot.Error(DebugMethod.REMOVE, nameof(variable), DebugError.NORMAL_DESERIALIZE);

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

                JsonConvert.SerializeObject(variables_list, Formatting.Indented).OverrideFile(FILE_PATH);

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
        /// Checks to see if a <see cref="Variable"/> already exists based on it's key.
        /// </summary>
        private bool Exists(string key)
        {
            return variables_dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Checks to see if the <see cref="Variable"/> has the right syntax before being loaded or modified.
        /// </summary>
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
        /// Converts a <see cref="TwitchMessage"/> recieved from Twitch and attempts to deserialize the body in to a <see cref="Variable"/>.
        /// </summary>
        private Variable MessageToVariable(TwitchMessage message)
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
        /// Attempts to deserialize extracted an extracted string from the body of a <see cref="TwitchMessage"/> into a <see cref="Variable"/>.
        /// </summary>
        private Variable MessageToVariable(DebugMethod method, TwitchMessage message, string variable_string)
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
        /// Parses through the body of a <see cref="TwitchMessage"/> for any posssible variable declarations between <see cref="lower_variable_search"/> and <see cref="upper_variable_indicator"/> and attempts to convert it to a <see cref="Variable"/>.
        /// </summary>
        public string ExtractVariables(string response, TwitchMessage message, out Variable[] variable_array)
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
        /// Returns a dictionary of all currently loaded <see cref="Variable"/>s.
        /// </summary>        
        public Dictionary<string, Variable> GetVariables()
        {
            return variables_dictionary;
        }

        #endregion
    }
}
