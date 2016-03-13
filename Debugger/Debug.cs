using System;

using TwitchChatBot.Enums.Debugger;
using TwitchChatBot.Extensions;

namespace TwitchChatBot.Debugger
{
    static class Debug
    {
        /// <summary>
        /// Prints a custom a header to the command line
        /// </summary>
        /// <param name="header">Text to print</param>
        public static void Header(string header)
        {
            SendHeader(ConsoleColor.Cyan, header);
        }

        /// <summary>
        /// Prints a custom sub header to the command line
        /// </summary>
        /// <param name="header"></param>
        public static void SubHeader(string header)
        {
            SendHeader(ConsoleColor.DarkCyan, "> " + header);
        }

        /// <summary>
        /// Prints a custom success header to the command line
        /// </summary>
        /// <param name="header"></param>
        public static void Success(string header)
        {
            SendHeader(ConsoleColor.Green, ">> " + header);
        }

        /// <summary>
        /// Prints a custom failed header to the command line
        /// </summary>
        /// <param name="header"></param>
        public static void Failed(string header)
        {
            SendHeader(ConsoleColor.Red, ">> " + header);
        }

        /// <summary>
        /// Prints a custom warning header to the command line
        /// </summary>
        /// <param name="header"></param>
        public static void Notify(string header)
        {
            SendHeader(ConsoleColor.Yellow, ">> " + header);
        }

        /// <summary>
        /// Prints a success header on a successful operation of adding/editing/removing a command, variable, or quote.
        /// </summary>
        /// <param name="operation">The operation what was being performed.</param>
        /// <param name="debug_class">The object that was operated on.</param>
        /// <param name="text">String to print in addition to the success message.</param>
        public static void Success(DebugMethod operation, DebugObject debug_class, string text)
        {
            string header = ">> Successfully {0} {1}: " + text;

            switch (operation)
            {                
                case DebugMethod.Add:
                case DebugMethod.Edit:
                case DebugMethod.Remove:
                case DebugMethod.Load:
                case DebugMethod.PreLoad:                
                    header = string.Format(header, operation.ToString().ToLower() + "ed the", debug_class.ToString().ToLower());
                    break;
                case DebugMethod.Separate:
                    header = string.Format(header, operation.ToString().ToLower() + "d the", debug_class.ToString().ToLower() + " response");
                    break;
                case DebugMethod.ParseKVP:
                    header = string.Format(header, "parsed", "the line into a key value pair");
                    break;
                case DebugMethod.ParseString:
                    header = string.Format(header, "parsed", "the line into a string");
                    break;
                default:
                    header = "";
                    break;
            }

            SendHeader(ConsoleColor.Green, header);
        }

        /// <summary>
        /// Prints a failed header on a successful operation of adding/editing/removing a command, variable, or quote.
        /// </summary>
        /// <param name="operation">The operation what was being performed.</param>
        /// <param name="debug_class">The object that was operated on.</param>
        /// <param name="error">The error that occured while trying to perform the operation.</param>
        public static void Failed(DebugMethod operation, DebugObject debug_class, DebugError error)
        {
            string header = ">> Failed to {0} {1}: " + new DebugErrorResponse().GetError(error);

            switch (operation)
            {
                case DebugMethod.Add:
                case DebugMethod.Edit:
                case DebugMethod.Remove:
                case DebugMethod.Load:
                case DebugMethod.PreLoad:
                case DebugMethod.Separate:
                    header = string.Format(header, operation.ToString().ToLower() + " the", debug_class.ToString().ToLower());
                    break;
                case DebugMethod.ParseKVP:
                    header = string.Format(header, "parse", "the line into a key value pair");
                    break;
                case DebugMethod.ParseString:
                    header = string.Format(header, "parse", "the line into a string");
                    break;
                default:
                    header = "";
                    break;
            }

            SendHeader(ConsoleColor.Red, header);
        }

        /// <summary>
        /// Prints a failed header on a successful operation of adding/editing/removing a command, variable, or quote. Used only for syntax errors.
        /// </summary>
        /// <param name="operation">The operation what was being performed.</param>
        /// <param name="debug_class">The object that was operated on.</param>
        /// <param name="syntax_class">The specific syntax object that was being operated on.</param>
        /// <param name="error">The error that occured while trying to perform the operation.</param>
        public static void Failed(DebugMethod operation, DebugObject debug_class, DebugObject syntax_class, SyntaxError error)
        {
            string header = ">> Incorrect {0} syntax: {1} " + new DebugErrorResponse().GetError(error);

            switch (operation)
            {
                case DebugMethod.SyntaxCheck:
                    header = string.Format(header, debug_class.ToString().ToLower(), syntax_class.ToString().ToLower());
                    break;
                default:
                    header = "";
                    break;
            }

            SendHeader(ConsoleColor.Red, header);
        }

        /// <summary>
        /// Print the header to the command line
        /// </summary>
        /// <param name="color">Color of the header</param>
        /// <param name="text">The text to print</param>
        private static void SendHeader(ConsoleColor color, string text)
        {
            if (!text.CheckString())
            {
                return;
            }

            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        /// <summary>
        /// Prints indented text below a header to provide additional useful information.
        /// </summary>
        /// <param name="sub_text">Text to print below the header.</param>
        public static void SubText(string sub_text)
        {
            if (!sub_text.CheckString())
            {
                return;
            }

            Console.ResetColor();

            Console.WriteLine("\t" + sub_text);
        }

        /// <summary>
        /// Prints indented text below a header to provide additional useful information.
        /// 
        /// </summary>
        /// <param name="array">Array of text to print below the header.</param>
        public static void SubText(string[] array)
        {
            if(array.Length == 0 || array == null)
            {
                return;
            }

            foreach(string element in array)
            {
                SubHeader(element);
            }
        }

        /// <summary>
        /// Inserts a plank line in the command line.
        /// This is 100% for code aesthetics, nothing else.
        /// </summary>
        public static void BlankLine()
        {
            Console.WriteLine();
        }
    }
}