using System;
using System.Collections;
using System.Reflection;


using TwitchBot.Extensions;



namespace TwitchBot.Debugger
{
    #region Enums

    public enum DebugMethod
    {
        LOAD,
        ADD,
        EDIT,
        REMOVE,
        UPDATE,
        MODIFY,        
        PARSE,
        APPLY,
        GET,
        SERIALIZE
    }

    public enum DebugMessageType
    {
        SUCCESS,
        WARNING,
        ERROR,
        NORMAL
    }

    #endregion

    static class DebugBot
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

        #region Print debug errors

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

        public static void PrintLine(DebugMessageType debug_message_type, string debug_string)
        {
            ConsoleColor color = GetDebugColor(debug_message_type);

            PrintLine(debug_string, color);
        }

        public static void PrintLine(DebugMessageType debug_message_type, DebugMethod debug_method, string debug_object, string debug_error = "")
        {
            string message,
                   template_message = GetDebugStringTemplate(debug_message_type);

            ConsoleColor color = GetDebugColor(debug_message_type);

            if(debug_message_type == DebugMessageType.WARNING)
            {
                message = string.Format(template_message, debug_object);
            }
            else
            {
                message = string.Format(template_message, GetMethodString(debug_message_type, debug_method), debug_object);
            }            

            if (debug_error.CheckString())
            {
                message += ": " + debug_error;
            }

            PrintLine(message, color);
        }

        public static string GetMethodString(DebugMessageType debug_method_type, DebugMethod debug_method)
        {
            string str = debug_method.ToString();

            if(debug_method_type == DebugMessageType.SUCCESS)
            {
                switch (debug_method)
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
            }           

            return str.ToLower();
        }

        private static ConsoleColor GetDebugColor(DebugMessageType debug_message_type)
        {
            ConsoleColor color;

            switch (debug_message_type)
            {
                case DebugMessageType.SUCCESS:
                    {
                        color = ConsoleColor.Green;
                    }
                    break;
                case DebugMessageType.WARNING:
                    {
                        color = ConsoleColor.Yellow;
                    }
                    break;
                case DebugMessageType.ERROR:
                    {
                        color = ConsoleColor.Red;
                    }
                    break;
                default:
                    {
                        color = ConsoleColor.Gray;
                    }                    
                    break;
            }

            return color;
        }

        private static string GetDebugStringTemplate(DebugMessageType debug_message_type)
        {
            string template;

            switch (debug_message_type)
            {
                case DebugMessageType.SUCCESS:
                    {
                        template = DebugMessage.SUCCESS;
                    }
                    break;
                case DebugMessageType.WARNING:
                    {
                        template = DebugMessage.WARNING;
                    }
                    break;
                case DebugMessageType.ERROR:
                    {
                        template = DebugMessage.ERROR;
                    }
                    break;
                default:
                    {
                        template = string.Empty;
                    }
                    break;
            }

            return template;
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