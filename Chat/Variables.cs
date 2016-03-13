using System;
using System.Collections.Generic;
using System.IO;

using TwitchChatBot.Enums.Debugger;
using TwitchChatBot.Clients;
using TwitchChatBot.Debugger;
using TwitchChatBot.Extensions;
using TwitchChatBot.Extensions.Files;

namespace TwitchChatBot.Chat
{
    class Variables
    {
        //indicates the bounds to parse between to find a variable 
        char lower_variable_indicator = '[',
             upper_variable_indicator = ']';

        //where to load everything from
        string file_path = Environment.CurrentDirectory + "/Variables.txt";

        //dictionaries to load the variables
        Dictionary<string, string> variables = new Dictionary<string, string>();
        Dictionary<string, string> preloaded_variables = new Dictionary<string, string>();

        public Variables()
        {
            Debug.BlankLine();
            Debug.Header("PreLoading variables");
            Debug.SubText("File path: " + file_path + Environment.NewLine);

            preloaded_variables = PreLoad(File.ReadAllLines(file_path));

            Debug.BlankLine();
            Debug.Header("Loading variables" + Environment.NewLine);

            foreach (KeyValuePair<string, string> pair in preloaded_variables)
            {
                Load(pair.Key, pair.Value);
            }
        }

        #region Load variables

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
                    Debug.SubHeader(" Preloading variable...");

                    int parse_point = line.IndexOf(" ");

                    if (parse_point != -1)
                    {
                        try
                        {
                            key = line.Substring(0, parse_point);
                            value = line.Substring(parse_point + 1);

                            //need to do a preliminary syntyax check for brackets
                            //if the varibale or value includes them it may bug out the debug text... ironic i know...
                            if (key.Contains("{") || key.Contains("}"))
                            {
                                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Variable, DebugObject.Variable, SyntaxError.BracketsNo);
                                Debug.SubText("Key: " + key);

                                continue;
                            }

                            if (value.Contains("{") || value.Contains("}"))
                            {
                                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Variable, DebugObject.Value, SyntaxError.BracketsNo);
                                Debug.SubText("Value: " + value);

                                continue;
                            }

                            preloaded_lines.Add(key, value);

                            Debug.Success(DebugMethod.PreLoad, DebugObject.Variable, line);
                            Debug.SubText("Key: " + key);
                            Debug.SubText("Value: " + value);
                        }
                        catch (Exception ex)
                        {
                            Debug.Failed(DebugMethod.PreLoad, DebugObject.Variable, DebugError.Exception);
                            Debug.SubText("Variable: " + line);
                            Debug.SubText("Exception: " + ex.Message);
                        }
                    }
                    else
                    {
                        Debug.Failed(DebugMethod.PreLoad, DebugObject.Variable, DebugError.Null);
                        Debug.SubText("Variable: " + line);
                    }
                }
            }

            return preloaded_lines;
        }

        /// <summary>
        /// Loads a command with a given response into the <see cref="variables"/> dictionary on launch.
        /// </summary>
        /// <param name="variable">Variable key to be added.</param>
        /// <param name="value">What is returned when the variable key is called.</param>
        /// <param name="message">(Optional parameter) Required to send a chat message or whisper by calling <see cref="Notify"/>.Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">(Optional parameter) Required to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        /// <returns></returns>
        private bool Load(string variable, string value, Message message = null, TwitchBot bot = null)
        {
            bool send_response = message != null && bot != null;

            Debug.SubHeader(" Loading variable...");

            //check to see iif the syntax is right
            if (!CheckSyntax(variable, value))
            {
                if (send_response)
                {
                    Notify.Failed(bot, DebugMethod.Add, DebugError.Syntax, message, variable);
                }

                Debug.Failed(DebugMethod.Load, DebugObject.Variable, DebugError.Syntax);
                Debug.SubText("Variable: " + variable);
                Debug.SubText("Value: " + value);

                return false;
            }

            //check to see if the user is trying to add a command that already exists
            if (Exists(variable))
            {
                if (send_response)
                {
                    Notify.Failed(bot, DebugMethod.Add, DebugError.ExistYes, message, variable);
                }

                Debug.Failed(DebugMethod.Load, DebugObject.Variable, DebugError.ExistYes);
                Debug.SubText("Variable: " + variable);

                return false;
            }

            try
            {
                //everyting went well? add the variable!
                variables.Add(variable, value);

                if (send_response)
                {
                    Notify.Success(bot, DebugMethod.Add, message, variable);
                }

                Debug.Success(DebugMethod.Load, DebugObject.Variable, variable);
                Debug.SubText("Variable: " + variable);
                Debug.SubText("Value: " + value);
            }
            catch (Exception ex)
            {
                //shit hit then fan, wtf happened
                if (send_response)
                {
                    Notify.Failed(bot, DebugMethod.Add, DebugError.Exception, message, variable);
                }

                Debug.Failed(DebugMethod.Load, DebugObject.Variable, DebugError.Exception);
                Debug.SubText("Variable: " + variable);
                Debug.SubText("Exception: " + ex.Message);

                return false;
            }

            return true;
        }

        #endregion

        #region Add, Edit, and Remove variables

        /// <summary>
        /// Parses the body of a <see cref="Message"/> for a command and attempts to add the specified command.
        /// Called from Twitch by using <code>!addvariable</code>.
        /// </summary>
        /// <param name="commands">Parses the body of a <see cref="Message"/> after the command and returns a <see cref="KeyValuePair{TKey, TValue}"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">Used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        public void Add(Commands commands, Message message, TwitchBot bot)
        {
            Debug.SubHeader(" Adding variable...");

            KeyValuePair<string, string> variable = commands.ParseCommandKVP(message);

            Add(variable.Key, variable.Value, message, bot);
        }

        /// <summary>
        /// Adds a variable with a given value into the <see cref="variables"/> dictionary in real time.
        /// </summary>
        /// <param name="variable">Variable key to be added</param>
        /// <param name="value">What is returned when the variable key is called</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">Used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        private void Add(string variable, string value, Message message = null, TwitchBot bot = null)
        {
            string text = variable + " " + value;

            if (Load(variable, value, message, bot))
            {
                text.AppendToFile(file_path);
            }
        }

        /// <summary>
        /// Parses the body of a <see cref="Message"/> for a command and attempts to edit the specified command.
        /// Called from Twitch by using <code>!editvariable</code>.
        /// </summary>
        /// <param name="commands">Parses the body of a <see cref="Message"/> after the command and returns a <see cref="KeyValuePair{TKey, TValue}"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">Used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        public void Edit(Commands commands, Message message, TwitchBot bot)
        {
            Debug.SubHeader(" Editing variable...");

            KeyValuePair<string, string> variable = commands.ParseCommandKVP(message);

            Edit(variable.Key, variable.Value, message, bot);
        }

        /// <summary>
        /// Edits the value of a given variable in the <see cref="variables"/> dictionary in real time.
        /// </summary>
        /// <param name="variable">Variable key to be edited.</param>
        /// <param name="value">What is returned when the variable key is called.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">Used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        private void Edit(string variable, string value, Message message, TwitchBot bot)
        {
            //check to see if the variable and value have the correct syntax
            if (!CheckSyntax(variable, value))
            {
                Notify.Failed(bot, DebugMethod.Edit, DebugError.Syntax, message, variable);

                Debug.Failed(DebugMethod.Edit, DebugObject.Variable, DebugError.Syntax);
                Debug.SubText("Variable: " + variable);
                Debug.SubText("Value: " + value);

                return;
            }

            //make sure the variable exists
            if (!Exists(variable))
            {
                Notify.Failed(bot, DebugMethod.Edit, DebugError.ExistNo, message, variable);

                Debug.Failed(DebugMethod.Edit, DebugObject.Variable, DebugError.ExistNo);
                Debug.SubText("Variable: " + variable);

                return;
            }

            try
            {
                //now try and edit the value of the variable
                string text = variable + " " + variables[variable];

                text.RemoveFromFile(file_path);

                variables[variable] = value;

                text = variable + " " + variables[variable];
                text.AppendToFile(file_path);                                   

                Notify.Success(bot, DebugMethod.Edit, message, variable);

                Debug.Success(DebugMethod.Edit, DebugObject.Variable, variable);
                Debug.SubText("Variable: " + variable);
                Debug.SubText("Value: " + value);
            }
            catch (Exception ex)
            {
                //something went down, abandon ship!
                Notify.Failed(bot, DebugMethod.Edit, DebugError.Exception, message, variable);

                Debug.Failed(DebugMethod.Edit, DebugObject.Variable, DebugError.Exception);
                Debug.SubText("Variable: " + variable);
                Debug.SubText("Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Parses the body of a <see cref="Message"/> for a variable and attempts to remove the specified variable.
        /// Called from Twitch by using <code>!removevariable</code>.
        /// </summary>
        /// <param name="commands">Parses the body of a <see cref="Message"/> after the command and returns a <see cref="KeyValuePair{TKey, TValue}"/>.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">Used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        public void Remove(Commands commands, Message message, TwitchBot bot)
        {
            Debug.SubHeader(" Removing variable...");

            string variable = commands.ParseCommandString(message);

            Remove(variable, message, bot);
        }

        /// <summary>
        /// Removed the specified variable from the <see cref="variables"/> dictionary in real time.
        /// </summary>
        /// <param name="variable">Variable key to be removed.</param>
        /// <param name="message">Contains the body of the message that is parsed. Also used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the message sender and room to send the chat message or whisper.</param>
        /// <param name="bot">Used to send a chat message or whisper by calling <see cref="Notify"/>. Contains the methods to send the chat message or whisper.</param>
        private void Remove(string variable, Message message, TwitchBot bot)
        {
            //check to see if the variable exists
            if (!Exists(variable))
            {
                Notify.Failed(bot, DebugMethod.Remove, DebugError.ExistNo, message, variable);

                Debug.Failed(DebugMethod.Remove, DebugObject.Variable, DebugError.ExistNo);
                Debug.SubText("Variable: " + variable);

                return;
            }

            try
            {
                //remove the variable!
                string text = variable + " " + variables[variable];

                text.RemoveFromFile(file_path);
                variables.Remove(variable);

                Notify.Success(bot, DebugMethod.Remove, message, variable);

                Debug.Success(DebugMethod.Remove, DebugObject.Variable, variable);
                Debug.SubText("Variable: " + variable);
            }
            catch (Exception ex)
            {
                //shit hit the fan
                Notify.Failed(bot, DebugMethod.Remove, DebugError.Exception, message, variable);

                Debug.Failed(DebugMethod.Remove, DebugObject.Variable, DebugError.Exception);
                Debug.SubText("Variable: " + variable);
                Debug.SubText("Exception: " + ex.Message);
            }

            return;
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
            return variables.ContainsKey(variable);
        }

        /// <summary>
        /// Checks to see if the variable array matches the proper syntax.
        /// </summary>
        /// <param name="array">Variable array to be processed.</param>
        /// <returns></returns>
        private bool CheckArraySyntax(string[] array)
        {
            //nothing was between the brackets
            if (array == null || array.Length == 0)
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Variable, DebugObject.Variable, SyntaxError.NullArray);

                return false;
            }

            //did not follow proper syntax
            if (array.Length != 2)
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Variable, DebugObject.Variable, SyntaxError.ArrayLength);
                Debug.SubText("Array length: " + array.Length);
                Debug.SubText("Desired length: 2:");

                return false;
            }

            return CheckSyntax(array[0], array[1]);
        }

        /// <summary>
        /// Checks to see if the variable and value match the proper syntax.
        /// </summary>
        /// <param name="variable">Variable key to be checked.</param>
        /// <param name="value">Value to be checked.</param>
        /// <returns></returns>
        private bool CheckSyntax(string variable, string value)
        {
            if (!variable.CheckString())
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Variable, DebugObject.Variable, SyntaxError.Null);
                Debug.SubText("Variable: null");

                return false;
            }

            if (!variable.StartsWith(lower_variable_indicator.ToString()) || !variable.EndsWith(upper_variable_indicator.ToString()))
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Variable, DebugObject.Variable, SyntaxError.SquareBracketsYes);
                Debug.SubText("Variable: " + variable);

                return false;
            }

            if (variable.Contains("{") || variable.Contains("}"))
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Variable, DebugObject.Variable, SyntaxError.BracketsNo);
                Debug.SubText("Variable: " + variable);

                return false;
            }

            if (!value.CheckString())
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Variable, DebugObject.Value, SyntaxError.Null);
                Debug.SubText("Variable: " + variable);
                Debug.SubText("Value: " + value);

                return false;
            }

            if (value.Contains("{") || value.Contains("}"))
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Variable, DebugObject.Response, SyntaxError.BracketsNo);
                Debug.SubText("Variable: " + variable);

                return false;
            }

            string _variable = variable.Substring(1, variable.Length - 2);

            if (_variable.Contains(lower_variable_indicator.ToString()) || _variable.Contains(upper_variable_indicator.ToString()))
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Variable, DebugObject.Variable, SyntaxError.SquareBracketsNo);
                Debug.SubText("Variable: " + variable);

                return false;
            }

            if (value.Contains(lower_variable_indicator.ToString()) || value.Contains(upper_variable_indicator.ToString()))
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Variable, DebugObject.Value, SyntaxError.SquareBracketsNo);
                Debug.SubText("Variable: " + variable);
                Debug.SubText("Value: " + value);

                return false;
            }

            if (_variable.Contains("="))
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Variable, DebugObject.Variable, SyntaxError.EqualSigns);
                Debug.SubText("Variable: " + variable);

                return false;
            }

            if (value.Contains("="))
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Variable, DebugObject.Value, SyntaxError.EqualSigns);
                Debug.SubText("Variable: " + variable);
                Debug.SubText("Value: " + value);

                return false;
            }

            if (_variable.Contains(" "))
            {
                Debug.Failed(DebugMethod.SyntaxCheck, DebugObject.Variable, DebugObject.Value, SyntaxError.Spaces);
                Debug.SubText("Variable: " + variable);

                return false;
            }

            return true;
        }

        #endregion

        #region String parsing        

        /// <summary>
        /// Loops through the body of the <see cref="Message"/> and attempts to add any variables found.
        /// </summary>
        /// <param name="body">Body of the <see cref="Message"/> to be processed</param>
        /// <returns></returns>
        public string ParseLoopAdd(string body)
        {
            int parse_start = 0,
                parse_end = 0;

            string extracted_variable;

            string[] array;

            bool in_bounds = true;

            while (parse_start < body.Length && in_bounds)
            {
                parse_start = body.IndexOf(lower_variable_indicator, parse_start);
                parse_end = body.IndexOf(upper_variable_indicator, parse_start + 1);

                //no { or }, no variable can exist, break the loop
                if (parse_start == -1 || parse_end == -1)
                {
                    break;
                }

                extracted_variable = body.Substring(parse_start + 1, parse_end - parse_start - 1);

                array = extracted_variable.StringToArray<string>('=');

                //there was extraced between the brackets
                if (array == null)
                {
                    parse_start += extracted_variable.Length + 2;

                    continue;
                }

                array[0] = lower_variable_indicator + array[0] + upper_variable_indicator;

                //user didn't specify a value for the variable
                if (array.Length == 1)
                {
                    Debug.Notify("Potential variable found (no value specified): " + array[0]);

                    //check the syntax to debug whether or not it will be replaced when called
                    CheckSyntax(array[0], "empty");

                    parse_start += extracted_variable.Length + 2;

                    continue;              
                }
                else
                {
                    Debug.Notify("Potential variable found (value specified): " + array[0]);

                    if (!CheckArraySyntax(array))
                    {
                        parse_start += extracted_variable.Length + 2;

                        continue;
                    }
                }

                Add(array[0], array[1]);

                body = body.Replace(lower_variable_indicator + extracted_variable + upper_variable_indicator, array[0]);

                //define the new parsing point
                parse_start += array[0].Length;
            }

            return body;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets the dictionary of the loaded variables
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetVariables()
        {
            return variables;
        }

        #endregion
    }
}
