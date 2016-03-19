using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using TwitchChatBot.Enums;

namespace TwitchChatBot.Extensions.Files
{
    static class FileExtensions
    {
        /// <summary>
        /// Appends a string to a text file.
        /// </summary>
        /// <param name="str">String to append.</param>
        /// <param name="path">Path to the text file.</param>
        public static void AppendToFile(this String str, string path)
        {
            using (StreamWriter writer = File.AppendText(path))
            {
                writer.WriteLine(Environment.NewLine + str);

                writer.Close();
            }
        }

        /// <summary>
        /// Removes a string from a text file.
        /// </summary>
        /// <param name="str">String to remove.</param>
        /// <param name="path">Path to the text file.</param>
        public static void RemoveFromFile(this String str, string path, FileFilter filter = FileFilter.Exact)
        {
            string temp_file = Path.GetTempFileName();

            IEnumerable<string> lines_to_keep;

            switch (filter)
            {
                case FileFilter.Exact:
                    lines_to_keep = File.ReadLines(path).Where(line => line != str);
                    break;
                case FileFilter.Contains:
                    lines_to_keep = File.ReadLines(path).Where(line => !line.Contains(str));
                    break;
                case FileFilter.EndsWith:
                    lines_to_keep = File.ReadLines(path).Where(line => !line.EndsWith(str));
                    break;
                case FileFilter.StartsWith:
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
