using System;
using System.Collections.Generic;
using System.ComponentModel;

using TwitchChatBot.Debugger;

namespace TwitchChatBot.Extensions
{
    static class UniversalExtensions
    {
        /// <summary>
        /// Checks to see if a string is null, empty, or is only whitespace.
        /// </summary>
        /// <param name="str">String to be checked.</param>
        /// <returns></returns>
        public static bool CheckString(this String str)
        {
            if (String.IsNullOrEmpty(str) || String.IsNullOrWhiteSpace(str) || str == null)
            {
                return false;
            }

            return true;
        }

        public static bool CheckArray(this Array array)
        {
            return array != null; 
        }

        /// <summary>
        /// Checks to see if an object can be convereted to certain type.
        /// </summary>
        /// <param name="obj">Object to be checked.</param>
        /// <param name="type">Type to be converted to.</param>
        /// <returns></returns>
        public static bool CanCovertTo(this object obj, Type type)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(type);

            return converter.IsValid(obj);
        }

        /// <summary>
        /// Gets the text between two characters. "starting_index" and "offset" are optional inputs.
        /// </summary>
        /// <param name="str">String to be parsed.</param>
        /// <param name="start">Character to start parsing from.</param>
        /// <param name="end">Character to stop parsing at.</param>
        /// <param name="starting_index">(Optional parameter) How far into the string to look for the first parsing point.</param>
        /// <param name="offset">(Optional parameter) How far from the first parsing point to start the substring.</param>
        /// <returns></returns>
        public static string TextBetween(this String str, char start, char end, int starting_index = 0, int offset = 0)
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
                Debug.Failed("Failed to find text between \"{start}\" and \"{end}\"");
            }

            return result;
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

            if(result.Count == 0)
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
    }
}
