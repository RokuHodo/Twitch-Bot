using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using TwitchChatBot.Enums.Extensions;

namespace TwitchChatBot.Extensions.Files
{
    static class FileExtensions
    {
        /// <summary>
        /// Overrides a file conttents with with new text to append
        /// </summary>
        /// <param name="str"></param>
        /// <param name="path"></param>
        public static void OverrideFile(this string str, string path)
        {
            path.ClearFile();
            str.AppendToFile(path);
        }

        /// <summary>
        /// Clears a text file of its content
        /// </summary>
        /// <param name="path"></param>
        public static void ClearFile(this string path)
        {
            File.Create(path).Close();
        }

        /// <summary>
        /// Appends a string to a text file.
        /// </summary>
        /// <param name="str">String to append.</param>
        /// <param name="path">Path to the text file.</param>
        public static void AppendToFile(this String str, string path)
        {
            using (StreamWriter writer = File.AppendText(path))
            {
                writer.WriteLine(str);

                writer.Close();
            }            
        }

        /// <summary>
        /// Removes a string from a text file.
        /// </summary>
        /// <param name="str">String to remove.</param>
        /// <param name="path">Path to the text file.</param>
        public static void RemoveFromFile(this string str, string path, FileSearch filter = FileSearch.Exact)
        {
            string temp_file = Path.GetTempFileName();

            IEnumerable<string> lines_to_keep;

            switch (filter)
            {
                case FileSearch.Exact:
                    lines_to_keep = File.ReadLines(path).Where(line => line != str);
                    break;
                case FileSearch.Contains:
                    lines_to_keep = File.ReadLines(path).Where(line => !line.Contains(str));
                    break;
                case FileSearch.EndsWith:
                    lines_to_keep = File.ReadLines(path).Where(line => !line.EndsWith(str));
                    break;
                case FileSearch.StartsWith:
                    lines_to_keep = File.ReadLines(path).Where(line => !line.StartsWith(str));
                    break;
                default:
                    lines_to_keep = File.ReadLines(path).Where(line => line != str);
                    break;
            }

            File.WriteAllLines(temp_file, lines_to_keep);

            File.Delete(path);
            File.Move(temp_file, path);
        }
    }
}
