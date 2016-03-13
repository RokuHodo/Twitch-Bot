using System;
using System.IO;
using System.Linq;

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
        public static void RemoveFromFile(this String str, string path)
        {
            string temp_file = Path.GetTempFileName();

            var lines_to_keep = File.ReadLines(path).Where(line => line != str);

            File.WriteAllLines(temp_file, lines_to_keep);

            File.Delete(path);
            File.Move(temp_file, path);
        }
    }
}
