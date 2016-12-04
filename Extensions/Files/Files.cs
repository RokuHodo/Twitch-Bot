using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using TwitchBot.Enums.Extensions;

namespace TwitchBot.Extensions.Files
{
    static class FileExtensions
    {
        /// <summary>
        /// Overrides a file contents with with new text to append
        /// </summary>
        public static void OverrideFile(this string str, string path)
        {
            path.ClearFile();
            str.AppendToFile(path);
        }

        /// <summary>
        /// Clears a text file of its content
        /// </summary>
        public static void ClearFile(this string path)
        {
            File.Create(path).Close();
        }

        /// <summary>
        /// Appends a string to a text file.
        /// </summary>
        public static void AppendToFile(this String str, string path)
        {
            using (StreamWriter writer = File.AppendText(path))
            {
                writer.WriteLine(str);

                writer.Close();
            }            
        }

        /// <summary>
        /// Removes a line of text from a text file with an optional search preference.
        /// </summary>
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
