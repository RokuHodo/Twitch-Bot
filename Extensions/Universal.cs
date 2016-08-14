using System;
using System.Collections.Generic;
using System.ComponentModel;

using TwitchChatBot.Debugger;
using TwitchChatBot.Enums.Extensions;

namespace TwitchChatBot.Extensions
{
    static class Universal
    {
        /// <summary>
        /// Converts the uptime fragment from <see cref="TimeSpan"/> to a displayable <see cref="string"/>.
        /// </summary>
        /// <param name="time">The value of the increment of time to get the string for.</param>
        /// <param name="time_string">The singular version of the time tier. Example: "hour".</param>
        /// <returns></returns>
        public static string GetTimeString(this int time, string time_string)
        {
            string to_return = time.ToString() + " " + time_string;

            if (time == 0)
            {
                return string.Empty;
            }
            else if (time == 1)
            {
                return to_return;
            }
            else
            {
                return to_return + "s";
            }
        }

        /// <summary>
        /// Checks to see if the value of an enum is within the defined range.
        /// </summary>
        /// <typeparam name="type">The enum to compare the _enum value against.</typeparam>
        /// <param name="_enum">The enum value to check.</param>
        /// <returns></returns>
        public static bool CheckEnumRange<type>(this Enum _enum)
        {
            int enum_size = Enum.GetNames(typeof(type)).Length - 1,
                permission_value = Convert.ToInt32(_enum);

            if (permission_value > enum_size)
            {
                return false;
            }

            return true;
        }

        #region Strings Extensions

        /// <summary>
        /// Checks to see if a string is null, empty, or is only whitespace.
        /// </summary>
        /// <param name="str">String to be checked.</param>
        /// <returns></returns>
        public static bool CheckString(this string str)
        {
            if (string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str))
            {
                return false;
            }

            return true;
        }

        public static string TextAfter(this string str, string find)
        {
            string result = string.Empty;

            int index = str.IndexOf(find);

            if(index != -1)
            {
                index += find.Length;

                result = str.Substring(index);
            }

            return result;
        }

        public static string TextBefore(this string str, string find)
        {
            string result = string.Empty;

            int index = str.IndexOf(find);

            if (index != -1)
            {
                result = str.Substring(0, index);
            }

            return result;
        }

        /// <summary>
        /// Gets the text between two characters at the first occurance.
        /// </summary>
        /// <param name="str">String to be parsed.</param>
        /// <param name="start">Character to start parsing from.</param>
        /// <param name="end">Character to stop parsing at.</param>
        /// <param name="starting_index">(Optional parameter) How far into the string to look for the first parsing point.</param>
        /// <param name="offset">(Optional parameter) How far from the first parsing point to start the substring.</param>
        /// <returns></returns>
        public static string TextBetween(this string str, char start, char end, int starting_index = 0, int offset = 0)
        {
            string result = "";

            int parse_start, parse_end;

            parse_start = str.IndexOf(start, starting_index) + 1;
            parse_end = str.IndexOf(end, parse_start);

            try
            {
                result = str.Substring(parse_start + offset, parse_end - parse_start - offset);
            }
            catch (Exception)
            {
                BotDebug.Error("Failed to find text between \"{start}\" and \"{end}\"");
            }

            return result;
        }

        /// <summary>
        /// Gets the text between two characters in a string with a specified search parameter.
        /// </summary>
        /// <param name="str">String to be parsed.</param>
        /// <param name="start">Character to start parsing from.</param>
        /// <param name="end">Character to stop parsing at.</param>
        /// <param name="search_type">The search filter to be applied when parsing for the text.</param>
        /// <param name="occurrence">Specifies which occurance of the extracted text to be returned. Only applicable when <see cref="StringSearch.Occurrence"/> is selected.</param>
        /// <returns></returns>
        public static string TextBetween(this string str, char start, char end, StringSearch search_type, int occurrence = 0)
        {
            string result = "";

            int parse_start = 0,
                parse_end = 0;

            try
            {
                switch (search_type)
                {
                    case StringSearch.First:
                        parse_start = str.IndexOf(start);
                        parse_end = str.IndexOf(end, parse_start + 1);

                        result = str.Substring(parse_start + 1, parse_end - parse_start - 1);
                        break;
                    case StringSearch.Last:
                        parse_start = str.LastIndexOf(start);
                        parse_end = str.IndexOf(end, parse_start + 1);

                        result = str.Substring(parse_start + 1, parse_end - parse_start - 1);
                        break;
                    case StringSearch.Occurrence:
                        for (int index = 0; parse_start < str.Length; index++)
                        {
                            parse_start = str.IndexOf(start, parse_start);
                            parse_end = str.IndexOf(end, parse_start + 1);

                            if (parse_start == -1 || parse_end == -1)
                            {
                                BotDebug.Error($"Failed to find the text between \"{start}\" and \"{end}\" at occurance = {occurrence}");

                                parse_start = -1;
                                parse_end = -1;

                                break;
                            }

                            if (index + 1 == occurrence)
                            {
                                result = str.Substring(parse_start + 1, parse_end - parse_start - 1);

                                break;
                            }

                            parse_start = parse_end + 1;
                        }
                        break;
                    default:
                        parse_start = str.IndexOf(start);
                        parse_end = str.IndexOf(end, parse_start + 1);

                        result = str.Substring(parse_start, parse_end - parse_start - 1);
                        break;
                }
            }
            catch (Exception ex)
            {
                BotDebug.Error($"Failed to find text between \"{start}\" and \"{end}\"");
                BotDebug.PrintLine("Exception: " + ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Wraps a string with the specified strings.
        /// </summary>
        /// <param name="str">String to wrap.</param>
        /// <param name="start">Text to be placed before the string.</param>
        /// <param name="end">Text to tbe placed after the string.</param>
        /// <returns></returns>
        public static string Wrap(this string str, string start, string end)
        {
            if (!str.StartsWith(start))
            {
                str = start + str;
            }

            if (!str.EndsWith(end))
            {
                str += end;
            }

            return str;
        }

        /// <summary>
        /// Removes white space from a string either at the begining, left, or both sides.
        /// </summary>
        /// <param name="str">String to be parsed for white space.</param>
        /// <param name="string_side">Specifies where the white space should be searched for. Default is <see cref="WhiteSpace.Both"/></param>
        /// <returns></returns>
        public static string RemoveWhiteSpace(this string str, WhiteSpace string_side = WhiteSpace.Both)
        {
            string temp = string.Empty;

            switch (string_side)
            {
                case WhiteSpace.Left:
                    for (int index = 0; index < str.Length; index++)
                    {
                        if (str[index].ToString().CheckString())
                        {
                            temp = str.Substring(index);

                            break;
                        }
                    }
                    break;
                case WhiteSpace.Right:
                    for (int index = str.Length; index > 0; index--)
                    {
                        if (str[index - 1].ToString().CheckString())
                        {
                            temp = str.Substring(0, index);

                            break;
                        }
                    }
                    break;
                case WhiteSpace.Both:
                    temp = str.RemoveWhiteSpace(WhiteSpace.Left);
                    temp = temp.RemoveWhiteSpace(WhiteSpace.Right);
                    break;
                case WhiteSpace.All:
                default:          
                    temp = str.Replace(" ", "");
                    break;
            }

            return temp;
        }

        /// <summary>
        /// Formats a string into a format that can then be serialized/deserialized. 
        /// </summary>
        /// <typeparam name="type">The format to preserialize the string as.</typeparam>
        /// <param name="str">String to be preserialized.</param>
        /// <returns></returns>
        public static string PreserializeAs<type>(this string str)
        {
            string preserialized_string = string.Empty;

            List<int> test = new List<int>();

            if (typeof(type).IsValueType || typeof(type) == typeof(string))
            {               
                preserialized_string = "{" + _Preserialize(str) + "}";
            }
            else if (typeof(type).IsArray || typeof(type).IsGenericType)
            {
                preserialized_string = "[" + _Preserialize_Array<type>(str) + "]";
            }
            else
            {
                preserialized_string = "{\"" + typeof(type).Name + "\": {" + _Preserialize(str) + "}}";
            }

            return preserialized_string;
        }

        /// <summary>
        /// Formats a string into a format that can then be serialized/deserialized with a specific label at the begining of the object. 
        /// </summary>
        /// <typeparam name="type">The format to preserialize the string as.</typeparam>
        /// <param name="str">String to be preserialized.</param>
        /// <param name="label">The label to be placed before the preserialized string.</param>
        /// <returns></returns>
        public static string PreserializeAs<type>(this string str, string label)
        {
            string preserialized_string = string.Empty;

            List<int> test = new List<int>();

            if (typeof(type).IsValueType || typeof(type) == typeof(string))
            {
                preserialized_string = "{\"" + label + "\": {" + _Preserialize(str) + "}}";
            }
            else if (typeof(type).IsArray || typeof(type).IsGenericType)
            {
                preserialized_string = "{\"" + label + "\": [" + _Preserialize_Array<type>(str) + "]}";
            }
            else
            {
                preserialized_string = "{\"" + label + "\": {" + _Preserialize(str) + "}}";
            }

            return preserialized_string;
        }

        /// <summary>
        /// Formats a string into a format that can be serialized/deserialized.
        /// </summary>
        /// <param name="str">String to be preserialized.</param>
        /// <returns></returns>
        private static string _Preserialize(string str)
        {
            string[] array = str.StringToArray<string>(',');

            string to_serialize = string.Empty;

            for (int index = 0; index < array.Length; index++)
            {
                string temp = array[index].RemoveWhiteSpace();

                int space_index = temp.IndexOf(' ');

                if (space_index == -1 || !temp.CheckString())
                {
                    continue;
                }

                string key = temp.Substring(0, space_index - 1).Wrap("\"", "\""),
                       value = temp.Substring(space_index + 1).Wrap("\"", "\"");

                key += ":";

                to_serialize += key + " " + value;

                if (index != array.Length - 1)
                {
                    to_serialize += ",";
                }
            }

            return to_serialize;
        }

        /// <summary>
        /// Formats a string into a format that can be serialized/deserialized as an array/list.
        /// </summary>
        /// <typeparam name="type">The format to preserialize the string as.</typeparam>
        /// <param name="str">String to be preserialized.</param>
        /// <returns></returns>
        private static string _Preserialize_Array<type>(string str)
        {
            string[] array = str.StringToArray<string>(',');

            string to_serialize = string.Empty;

            for (int index = 0; index < array.Length; index++)
            {
                string temp = array[index].RemoveWhiteSpace();

                if(typeof(type) == typeof(string[]) || typeof(type) == typeof(List<string>))
                {
                    temp = temp.Wrap("\"", "\"");
                }                

                to_serialize += temp;

                if (index != array.Length - 1)
                {
                    to_serialize += ", ";
                }
            }

            return to_serialize;
        }

        #endregion

        #region Array Extensions

        /// <summary>
        /// Checks to see if an array is null.
        /// </summary>
        /// <param name="array">Array to be checked.</param>
        /// <returns></returns>
        public static bool CheckArray(this Array array)
        {
            return array != null; 
        }

        /// <summary>
        /// Converts a string into an array using user specified break points. 
        /// Whitespace lines are ignored and not added to the array.
        /// </summary>
        /// <typeparam name="type">The type of array to be returned</typeparam>
        /// <param name="str">The string to be parsed into an array</param>
        /// <param name="parse_point">Where a new array element will be defined</param>
        /// <param name="print_debug_text">Determine if the debug print should be printed</param>
        /// <returns></returns>
        public static type[] StringToArray<type>(this String str, char parse_point, bool print_debug_text = false)
        {
            if (!str.CheckString())
            {
                return null;
            }

            string[] array = str.Split(parse_point);

            List<type> result = new List<type>();

            bool first_failed_conversion = true;

            for (int index = 0; index < array.Length; index++)
            {
                try
                {
                    result.Add((type)Convert.ChangeType(array[index], typeof(type)));
                }
                catch (FormatException)
                {
                    if (print_debug_text)
                    {
                        if (first_failed_conversion)
                        {
                            Console.WriteLine();

                            first_failed_conversion = false;
                        }

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\t\tError: could not convert array element \"{0}\" from \"{1}\" to \"{2}\" at index \"{3}\"", array[index], typeof(string).Name, typeof(type).Name, index);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }
            }

            type[] _result;

            if (result.Count == 0)
            {
                _result = null;
            }
            else
            {
                _result = result.ToArray();
            }


            if (_result.Length != array.Length && print_debug_text)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n\t\tError: some elements from the array could not be converted from \"{0}\" to \"{1}\"", typeof(string).Name, typeof(type).Name);

                Console.Write("\n\t\tInput array:\t");
                foreach (string element in array)
                {
                    Console.Write("{0} ", element);
                }

                Console.Write("\n\t\tReturned array:\t");
                foreach (type element in _result)
                {
                    Console.Write("{0} ", element);
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            else if (_result.Length == array.Length && print_debug_text)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n\t\tConversion from \"{0}\" to \"{1}\" successful!\n", typeof(string[]).Name, typeof(type[]).Name);
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            return _result;
        }

        #endregion

        #region Conversion Extensions

        /// <summary>
        /// Checks to see if an object can be convereted to certain type.
        /// </summary>
        /// <param name="obj">Object to be checked.</param>
        /// <param name="type">Type to be converted to.</param>
        /// <returns></returns>
        public static bool CanCovertTo<type>(this object obj)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(type));

            return converter.IsValid(obj);
        }

        #endregion

    }
}
