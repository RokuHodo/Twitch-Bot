using System;
using System.Collections;
using System.Reflection;

using TwitchBot.Enums.Debugger;
using TwitchBot.Extensions;

namespace TwitchBot.Debugger
{
    static class DebugBot
    {
        #region Debug headers to the command line

        /// <summary>
        /// Prints text to the command line as dark cyan.
        /// </summary>        
        public static void SubHeader(string header)
        {
            PrintLine(header, ConsoleColor.DarkCyan);
        }

        #endregion

        #region Debug messages to command line

        /// <summary>
        /// Prints a success debug message to the command line following a template.
        /// [ Success ] Successfully (method) the (obj)
        /// </summary>
        public static void Success(DebugMethod method, string obj)
        {
            string message = string.Empty;

            ConsoleColor color = ConsoleColor.Green;

            message = "[ Success ] Successfully " + GetSuccessMethodString(method).ToLower() + " the " + obj;

            PrintLine(message, color);
        }

        /// <summary>
        /// Gets the proper conjugation of the <see cref="DebugMethod"/> in the present tense.
        /// </summary>
        public static string GetSuccessMethodString(DebugMethod method)
        {
            string str = method.ToString();

            switch (method)
            {
                //anything that ends in a consonant 
                case DebugMethod.ADD:
                case DebugMethod.EDIT:
                case DebugMethod.LOAD:
                    {
                        str += "ed";
                    }
                    break;
                //anything that ends in a vowel
                case DebugMethod.PARSE:
                case DebugMethod.REMOVE:
                case DebugMethod.SERIALIZE:
                case DebugMethod.UPDATE:
                    {
                        str += "d";
                    }
                    break;
                //anything that ends in "y"
                case DebugMethod.MODIFY:
                case DebugMethod.APPLY:
                    {
                        str = str.TextBefore("y") + "ied";
                    }
                    break;
                //any special verbs
                case DebugMethod.GET:
                    {
                        str = "got";
                    }
                    break;
                default:
                    break;
            }

            return str;
        }


        /// <summary>
        /// Prints a custom success debug message to the command line.
        /// [ Success ] (message)
        /// </summary>
        public static void Success(string message)
        {
            if (!message.CheckString())
            {
                return;
            }

            ConsoleColor color = ConsoleColor.Green;

            message = "[ Success ] " + message;

            PrintLine(message, color);
        }

        /// <summary>
        /// Prints a custom warning debug message to the command line.
        /// [ Warning ] (message)
        /// </summary>
        public static void Warning(string message)
        {
            if (!message.CheckString())
            {
                return;
            }

            ConsoleColor color = ConsoleColor.Yellow;

            message = "[ Warning ] " + message;

            PrintLine(message, color);
        }

        /// <summary>
        /// Prints a custom notify debug message to the command line.
        /// [ Notice ] (message)
        /// </summary>
        public static void Notify(string message, string notice = "Notice", ConsoleColor color = ConsoleColor.Cyan)
        {
            if (!message.CheckString())
            {

            }

            if (notice.CheckString())
            {
                message = "[ " + notice + " ] " + message;
            }

            PrintLine(message, color);
        }

        /// <summary>
        /// Prints a error debug message to the command line following a template.
        /// [ Error ] Failed to (method) the (obj) : (error)
        /// </summary>
        public static void Error(DebugMethod method, string obj, string error)
        {
            string message = string.Empty;

            ConsoleColor color = ConsoleColor.Red;

            message = "[ Error ] Failed to " + method.ToString().ToLower() + " the " + obj;

            if(error.CheckString())
            {
                message += ": " + error;
            }

            PrintLine(message, color);
        }

        /// <summary>
        /// Prints a custom error debug message to the command line.
        /// [ Error ] Failed to (method) the (obj) : (error)
        /// </summary>
        public static void Error(string message)
        {
            ConsoleColor color = ConsoleColor.Red;

            message = "[ Error ] " + message;

            PrintLine(message, color);
        }

        #endregion
                
        #region Printing to the command line

        /// <summary>
        /// Prints a line of text to the command line with an optional sepecified color. No carriage return.
        /// </summary>
        public static void Print(string text, ConsoleColor color = ConsoleColor.Gray)
        {
            PrintFinal(text, color);
        }

        /// <summary>
        /// Prints a line of text to the command line preceeded with a label and with an optional sepecified color. No carriage return.
        /// </summary>
        public static void Print(string label, string text, ConsoleColor color = ConsoleColor.Gray)
        {
            string message = "{0,-20} {1,-20}";

            message = string.Format(message, label, text);

            PrintFinal(message, color);
        }

        /// <summary>
        /// Prints a line of text to the command line with an optional sepecified color. Returns to the start oif a new line.
        /// </summary>
        public static void PrintLine(string text, ConsoleColor color = ConsoleColor.Gray)
        {
            PrintFinal(text, color, true);
        }

        /// <summary>
        /// Prints a line of text to the command line preceeded with a label and with an optional sepecified color. Returns to the start oif a new line.
        /// </summary>
        public static void PrintLine(string label, string text, ConsoleColor color = ConsoleColor.Gray)
        {
            string message = "{0,-20} {1,-20}";

            message = string.Format(message, label, text);

            PrintFinal(message, color, true);
        }

        /// <summary>
        /// Prints a line of text to the command line with a specified color and if it should return to a new line after printing.
        /// </summary>
        private static void PrintFinal(string text, ConsoleColor color, bool new_line = false)
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
        /// Prints a blank line to the command line.
        /// </summary>
        public static void BlankLine()
        {
            Console.WriteLine();
        }

        #endregion        

        #region Object dump

        /// <summary>
        /// Prints all properties and fields of an object and all sub objects.
        /// </summary>        
        public static void PrintObject(object obj)
        {
            PrintObject(null, obj);
        }

        /// <summary>
        /// Prints all properties and fields of an object and all sub objects with a specified prefix.
        /// </summary>
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
        /// Converts the object into a printable string.
        /// </summary>        
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
    }
}