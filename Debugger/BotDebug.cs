using System;

using TwitchChatBot.Enums.Debug;
using TwitchChatBot.Extensions;

using System.Reflection;
using System.Collections;

namespace TwitchChatBot.Debugger
{
    static class BotDebug
    {
        public static bool debug = true;

        private static int indent = 0;

        /// <summary>
        /// Declares the start of a new debug block to make sure the indentation is correct.
        /// </summary>
        public static void BlockBegin()
        {
            indent = 0;
        }

        /// <summary>
        /// Ends a debug block. Resets the indentation.
        /// </summary>
        public static void BlockEnd()
        {
            indent = 0;
        }

        #region Print custom debug errors

        /// <summary>
        /// Prints a custom a header to the command line
        /// </summary>
        /// <param name="header">Text to print</param>
        public static void Header(string header)
        {
            PrintLine(header, ConsoleColor.Cyan);
            ++indent;
        }

        /// <summary>
        /// Prints a custom sub header to the command line
        /// </summary>
        /// <param name="header"></param>
        public static void SubHeader(string header)
        {
            PrintLine(header, ConsoleColor.DarkCyan);
        }

        /// <summary>
        /// Prints a custom success header to the command line
        /// </summary>
        /// <param name="header"></param>
        public static void Success(string text)
        {            
            PrintLine(text, ConsoleColor.Green);
        }

        /// <summary>
        /// Prints a custom failed header to the command line
        /// </summary>
        /// <param name="header"></param>
        public static void Error(string text)
        {
            PrintLine(text, ConsoleColor.Red);
        }

        /// <summary>
        /// Prints a custom warning header to the command line
        /// </summary>
        /// <param name="header"></param>
        public static void Notify(string text)
        {
            PrintLine(text, ConsoleColor.Yellow);
        }

        #endregion

        #region Print debug errors

        /// <summary>
        /// Prints a success header on a successful operation of adding/editing/removing a command, variable, or quote.
        /// </summary>
        /// <param name="operation">The operation what was being performed.</param>
        /// <param name="debug_object">The object that was operated on.</param>
        /// <param name="description">String to print in addition to the success message.</param>
        public static void Success(DebugMethod operation, DebugObject debug_object, string description)
        {
            string header = "Successfully ",
                   operation_string = operation.ToString().ToLower(),
                   debug_object_string = debug_object.ToString().ToLower().Replace("_", " ");

            switch (operation)
            {                
                case DebugMethod.Add:
                case DebugMethod.Edit:                
                case DebugMethod.Load:
                case DebugMethod.PreLoad:                                                    
                    header += operation_string + "ed the " + debug_object_string;
                    break;
                case DebugMethod.Update:
                case DebugMethod.Remove:
                case DebugMethod.Separate:
                case DebugMethod.Serialize:
                case DebugMethod.Deserialize:
                case DebugMethod.Modify:
                case DebugMethod.Retrieve:
                    header += operation.ToString().ToLower() + "d the " + debug_object_string;
                    break;
                case DebugMethod.ParseKVP:
                    header += "parsed the line into a key value pair";
                    break;
                case DebugMethod.ParseString:
                    header += "parsed the line into a string";
                    break;
                default:
                    header = "";
                    break;
            }

            if (header.CheckString())
            {
                header += ": " + description;
            }

            PrintLine(header, ConsoleColor.Green);
        }

        /// <summary>
        /// Prints a failed header on a successful operation of adding/editing/removing a command, variable, or quote.
        /// </summary>
        /// <param name="operation">The operation what was being performed.</param>
        /// <param name="debug_object">The object that was operated on.</param>
        /// <param name="error">The error that occured while trying to perform the operation.</param>
        public static void Error(DebugMethod operation, DebugObject debug_object, DebugError error)
        {
            string header = "Failed to ";

            switch (operation)
            {
                case DebugMethod.Add:
                case DebugMethod.Edit:
                case DebugMethod.Remove:
                case DebugMethod.Load:
                case DebugMethod.PreLoad:
                case DebugMethod.Separate:
                case DebugMethod.Serialize:
                case DebugMethod.Deserialize:
                case DebugMethod.Update:
                case DebugMethod.Modify:
                case DebugMethod.Retrieve:
                    header += operation.ToString().ToLower() + " the " + debug_object.ToString().Replace("_", " ").ToLower();
                    break;
                case DebugMethod.ParseKVP:
                    header += "parse the line into a key value pair";
                    break;
                case DebugMethod.ParseString:
                    header += "parse the line into a string";
                    break;
                default:
                    header = "";
                    break;
            }

            if (header.CheckString())
            {
                header += ": " + new ErrorResponse().GetError(error);
            }            

            PrintLine(header, ConsoleColor.Red);
        }

        /// <summary>
        /// Prints a failed header on a successful operation of adding/editing/removing a command, variable, or quote. Used only for syntax errors.
        /// </summary>
        /// <param name="operation">The operation what was being performed.</param>
        /// <param name="debug_object">The object that was operated on.</param>
        /// <param name="syntax_class">The specific syntax object that was being operated on.</param>
        /// <param name="error">The error that occured while trying to perform the operation.</param>
        /// <param name="value">Optional parameter used when a <see cref="Enum"/> range error occurs.</param>
        public static void SyntaxError(DebugObject debug_object, DebugObject syntax_class, SyntaxError error, int value = 0)
        {
            string header = "Incorrect " + debug_object.ToString().ToLower() + " syntax: " + syntax_class.ToString().ToLower() + " " + new ErrorResponse().GetError(error, value);

            PrintLine(header, ConsoleColor.Red);
        }

        #endregion

        #region Print to the command line

        /// <summary>
        /// Prints a message to the command line without starting a new line.
        /// </summary>
        /// <param name="text">Text to be printed.</param>
        /// <param name="color">Color of the text. Default is <see cref="ConsoleColor.Gray"/></param>
        public static void Print(string text, ConsoleColor color = ConsoleColor.Gray)
        {
            string tab = GetTabIndent();

            _Print(tab + text, color);
        }

        /// <summary>
        /// Prints a message to the command line and starts a new line after printing.
        /// </summary>
        /// <param name="text">Text to be printed.</param>
        /// <param name="color">Color of the text. Default is <see cref="ConsoleColor.Gray"/></param>
        public static void PrintLine(string text, ConsoleColor color = ConsoleColor.Gray)
        {
            string tab = GetTabIndent();

            _Print(tab + text, color, true);
        }

        /// <summary>
        /// Prints text to the command line with a specified label infront of it without starting a new line after.
        /// </summary>
        /// <param name="label">Label of the text</param>
        /// <param name="text">Text to be printed.</param>
        /// <param name="color">Color of the text. Default is <see cref="ConsoleColor.Gray"/></param>
        public static void Print(string label, string text, ConsoleColor color = ConsoleColor.Gray)
        {
            string tab = GetTabIndent();

            string _text = "{0,-15} {1,-15}";

            _text = label.CheckString() ? string.Format(_text, label, text) : text;

            _Print(tab + _text, color);
        }

        /// <summary>
        /// Prints text to the command line with a specified label infront of it and starts a new line after.
        /// </summary>
        /// <param name="label">Label of the text</param>
        /// <param name="text">Text to be printed.</param>
        /// <param name="color">Color of the text. Default is <see cref="ConsoleColor.Gray"/></param>
        public static void PrintLine(string label, string text, ConsoleColor color = ConsoleColor.Gray)
        {
            string tab = GetTabIndent();

            string _text = "{0,-15} {1,-15}";

            _text = label.CheckString() ? string.Format(_text, label, text) : text;

            _Print(tab + _text, color, true);
        }

        /// <summary>
        /// Prints text to the command line.
        /// </summary>
        /// <param name="text">Text to print.</param>
        /// <param name="color">Color of the text. Default is <see cref="ConsoleColor.Gray"/></param>
        /// <param name="new_line">Determines whether or not to start a new line after the text is printed.</param>
        private static void _Print(string text, ConsoleColor color, bool new_line = false)
        {
            Console.ForegroundColor = color;

            if (new_line)
            {
                Console.WriteLine(text);
            }
            else
            {
                Console.Write(text);
            }
            
            Console.ResetColor();
        }

        /// <summary>
        /// Inserts a plank line in the command line.
        /// This is 100% for code aesthetics, nothing else.
        /// </summary>
        public static void BlankLine()
        {
            Console.WriteLine();
        }

        #endregion        

        #region Object dump

        /// <summary>
        /// Dumps all members of an object and prints them to the command line.
        /// </summary>
        /// <param name="obj">The object to dump.</param>
        public static void PrintObject(object obj)
        {
            PrintObject(null, obj);
        }

        /// <summary>
        /// Dumps all members of an object and prints them to the command line.
        /// </summary>
        /// <param name="prefix">The label to be printed before the members as a title.</param>
        /// <param name="obj">The object to dump.</param>
        private static void PrintObject(string prefix, object obj)
        {
            if (obj == null || obj is ValueType || obj is string)
            {
                PrintLine(prefix, GetPrintValue(obj));
            }
            else if (obj is IEnumerable)
            {
                foreach (object element in (IEnumerable)obj)
                {
                    PrintObject(prefix, element);
                }
            }
            else
            {
                MemberInfo[] members = obj.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);

                foreach (MemberInfo member in members)
                {
                    FieldInfo field_info = member as FieldInfo;
                    PropertyInfo property_info = member as PropertyInfo;

                    if (field_info != null || property_info != null)
                    {
                        Type type = field_info != null ? field_info.FieldType : property_info.PropertyType;

                        object _obj = field_info != null ? field_info.GetValue(obj) : property_info.GetValue(obj, null);

                        string value = GetPrintValue(_obj);

                        if(value == string.Empty)
                        {
                            BlankLine();
                            SubHeader(member.Name);
                        }
                        else
                        {
                            PrintLine(member.Name, value);
                        }                        

                        if (!(type.IsValueType || type == typeof(string)) && _obj != null)
                        {
                            PrintObject(string.Empty, _obj);
                        }                        
                    }
                }
            }
        }

        /// <summary>
        /// Gets the value of the member to be printed.
        /// </summary>
        /// <param name="obj">The object to get the value of.</param>
        /// <returns></returns>
        private static string GetPrintValue(object obj)
        {
            if (obj == null)
            {
                return "null";
            }
            else if (obj is DateTime)
            {
                return ((DateTime)obj).ToShortDateString();
            }
            else if (obj is ValueType || obj is string)
            {
                return obj.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        #endregion

        #region Other functions

        /// <summary>
        /// Gets how many indentations there should be before a line of text.
        /// </summary>
        /// <returns></returns>
        private static string GetTabIndent()
        {
            string tab = string.Empty;

            for (int index = 0; index < indent; index++)
            {
                tab += "\t";
            }

            return tab;
        }

        #endregion
    }
}