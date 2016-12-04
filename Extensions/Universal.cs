using System;
using System.Collections.Generic;
using System.ComponentModel;

using TwitchBot.Debugger;
using TwitchBot.Enums.Extensions;

namespace TwitchBot.Extensions
{
    static class Universal
    {
        /// <summary>
        /// Calculates the percentage of two numbers and compares the percent to the minimum allowable percentage.
        /// Checks to see if 100 * numerator / denominator exceeds max_allowable_percent
        /// </summary>
        public static bool ExceedsMaxAllowablePercent(this int numerator, int denominator, int max_allowable_percent)
        {
            int percent = 100 * numerator / denominator;

            return Convert.ToInt32(percent) < max_allowable_percent;
        }

        /// <summary>
        /// Converts the uptime fragment from <see cref="TimeSpan"/> to a displayable <see cref="string"/>.
        /// </summary>
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
        public static bool CheckString(this string str)
        {
            if (string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the text after a certain part of a string.
        /// Returns <see cref="string.Empty"/> if the string index cannot be found.
        /// </summary>
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

        /// <summary>
        /// Gets the text before a certain part of a string.
        /// Returns <see cref="string.Empty"/> if the string index cannot be found.
        /// </summary>
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
        /// The starting index can be specified.
        /// The offset specifies how far into the sub string to return.
        /// </summary>
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
            catch (Exception exception)
            {
                DebugBot.Error("Failed to find text between \"" + start + "\" and \"" + end + "\"");
                DebugBot.PrintLine(nameof(str), str);
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }

            return result;
        }

        /// <summary>
        /// Gets the text between two characters with a specified search parameter.
        /// A specific occurance can be returned if <see cref="StringSearch.Occurrence"/> is selected and the zero based occurance is specified.
        /// </summary>
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
                                DebugBot.Error("Failed to find the text between \"" + start + "\" and \"" + end + "\" at occurance = \"" + occurrence + "\"");

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
            catch (Exception exception)
            {
                DebugBot.Error("Failed to find text between \"" + start + "\" and \"" + end + "\"");
                DebugBot.PrintLine(nameof(exception), exception.Message);
            }

            return result;
        }

        /// <summary>
        /// Wraps a string with the specified strings.
        /// </summary>
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
        /// Removes padding from the left, right, both sides of a string.
        /// </summary>
        public static string RemovePadding(this string str, Padding string_side = Padding.Both)
        {
            string temp = string.Empty;

            switch (string_side)
            {
                case Padding.Left:
                    for (int index = 0; index < str.Length; index++)
                    {
                        if (str[index].ToString().CheckString())
                        {
                            temp = str.Substring(index);

                            break;
                        }
                    }
                    break;
                case Padding.Right:
                    for (int index = str.Length; index > 0; index--)
                    {
                        if (str[index - 1].ToString().CheckString())
                        {
                            temp = str.Substring(0, index);

                            break;
                        }
                    }
                    break;
                case Padding.Both:
                    temp = str.RemovePadding(Padding.Left);
                    temp = temp.RemovePadding(Padding.Right);
                    break;
                default:
                    temp = str.RemovePadding(Padding.Both);
                    break;
            }

            return temp;
        }

        public static string RemoveWhiteSpace(this string str)
        {
            return str.Replace(" ", "");
        }

        /// <summary>
        /// Formats a string into a format that can then be deserialized with an optional label at the begining.
        /// </summary>
        public static string PreserializeAs<type>(this string str, string label = "")
        {
            string preserialized = string.Empty;

            //integrak types, floating point types, decimal, bool, user defined structs, and strings
            if (typeof(type).IsValueType || typeof(type) == typeof(string))
            {
                preserialized = "{" + Preserialize(str) + "}";                      //formatted as {str}
            }
            //arrays and generic collections
            else if (typeof(type).IsArray || typeof(type).IsGenericType)
            {
                preserialized = "[" + Preserialize_Array<type>(str) + "]";          //formatted as [str]
            }
            //everything else is assumed to be an object/class with associated fields
            else
            {
                if (!label.CheckString())
                {
                    label = typeof(type).Name;
                }                    

                preserialized = "{" + Preserialize(str) + "}";                     //formatted as {"label": str}
            }

            if (label.CheckString())
            {
                preserialized = "{\"" + label + "\":" + preserialized + "}";       //formatted as {"label": <preserialized>}
            }

            return preserialized; 
        }

        /// <summary>
        /// Formats a string into a format that can be deserialized.
        /// </summary>
        private static string Preserialize(string str)
        {
            string preserialized = string.Empty;

            string[] array = str.StringToArray<string>('|');                    //input as key: value | key: value | key: value ...

            for (int index = 0; index < array.Length; ++index)
            {
                string element = array[index].RemovePadding(),               //" key: value " -> "key: value"
                       key = element.TextBefore(":").RemovePadding(),        //<key> = key
                       value = element.TextAfter(":").RemovePadding();       //<value> = value

                //make sure both the key and the value are valid
                if(!key.CheckString() || !value.CheckString())
                {
                    continue;
                }

                key = key.Wrap("\"", "\"") + ":";                               //formatted as "<key>":
                value = value.Wrap("\"", "\"");                                 //formatted as "<value>"

                preserialized += key + " " + value;                             //formatted as "<key>": "<value>"

                //there are more elements in the set, append ', '
                if (index != array.Length - 1)
                {
                    preserialized += ", ";                                      //formatted as "<key>": "<value>", 
                }
            }

            return preserialized;                                               //formatted as "<key>": "<value>", ..., "<key>": "<value>"
        }

        /// <summary>
        /// Formats a string so it can be deserialized as an array/list.
        /// </summary>
        private static string Preserialize_Array<type>(string str)
        {
            string[] array = str.StringToArray<string>(',');                                    //input as value, value, value, value, value, 

            string to_serialize = string.Empty;

            for (int index = 0; index < array.Length; index++)
            {
                string temp = array[index].RemovePadding();

                //array/list of strings, wrap all values in quotes
                if(typeof(type) == typeof(string[]) || typeof(type) == typeof(List<string>))
                {
                    temp = temp.Wrap("\"", "\"");                                               //formatted as "<value>"
                }                

                to_serialize += temp;

                if (index != array.Length - 1)
                {
                    to_serialize += ", ";                                                       //formatted as <temp>,                                        
                }
            }

            return to_serialize;                                                                //formatted as <temp>, <temp>,  ..., <temp>
        }

        #endregion

        #region Array Extensions

        /// <summary>
        /// Checks to see if an array is null.
        /// </summary>
        public static bool CheckArray(this Array array)
        {
            return array != null; 
        }

        /// <summary>
        /// Converts a string into an array using user specified break points. 
        /// Whitespace lines are ignored and not added to the array.
        /// </summary>
        public static type[] StringToArray<type>(this string str, char parse_point, bool print_debug_text = false)
        {
            if (!str.CheckString())
            {
                return default(type[]);
            }

            string[] array = str.Split(parse_point);

            List<type> result = new List<type>();

            int index = 0;
            foreach(string element in array)
            {
                if(!element.CanCovertTo<type>())
                {
                    continue;
                }

                try
                {
                    result.Add((type)Convert.ChangeType(element, typeof(type)));
                }
                catch (Exception exception)
                {
                    if (print_debug_text)
                    {
                        DebugBot.Error("Could not convert array element \"" + element + "\" from \"" + typeof(string).Name + "\" to \"" + typeof(type).Name + "\" at index \"" + index + "\"");
                        DebugBot.PrintLine(nameof(exception), exception.Message);
                    }                    
                }

                ++index;
            }

            if (print_debug_text)
            {
                if (result.Count != array.Length)
                {
                    DebugBot.Warning("Some elements from the array could not be converted from \"" + typeof(string).Name + "\" to \"" + typeof(type).Name + "\"");
                }
                else
                {
                    DebugBot.Success("Conversion from \"" + typeof(string[]).Name + "\" to \"" + typeof(type[]).Name + "\" successful!\n");
                }
            }
            

            return result.Count == 0 ? default(type[]) : result.ToArray();
        }

        #endregion

        #region Conversion Extensions

        /// <summary>
        /// Checks to see if an object can be convereted to certain type.
        /// </summary>
        public static bool CanCovertTo<type>(this object obj)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(type));

            return converter.IsValid(obj);
        }

        /// <summary>
        /// Checks to see if a string contains a value of an array.
        /// </summary>
        /// <typeparam name="type"></typeparam>
        public static bool Contains<type>(this string str, type[] array)
        {
            foreach(type value in array)
            {
                if (str.Contains(value.ToString()))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

    }
}
