using System;
using System.IO;
using System.Linq;

namespace CoopClient
{
    internal static class Logger
    {
        public enum LogLevel
        {
            Normal,
            Server,
            Custom
        }

        private static string GetFilePath(LogLevel level, string filepath)
        {
            string newFilePath = null;

            switch (level)
            {
                case LogLevel.Normal:
                    newFilePath = "scripts\\CoopLog.txt";
                    break;
                case LogLevel.Server:
                    newFilePath = $"scripts\\resources\\{Main.MainSettings.LastServerAddress.Replace(":", ".")}\\CoopLog.txt";
                    break;
                case LogLevel.Custom:
                    if (string.IsNullOrEmpty(filepath))
                    {
                        goto case LogLevel.Normal;
                    }

                    newFilePath = filepath;
                    break;
            }

            return newFilePath;
        }

        public static void Write(string message, LogLevel level = LogLevel.Normal, string filepath = null)
        {
            string newFilePath = GetFilePath(level, filepath);

            try
            {
                if (File.Exists(newFilePath))
                {
                    // Check if the rows are under 20 and delete the first 10 if so to avoid large logs
                    // Firstly get all lines
                    string[] oldLines = File.ReadAllLines(newFilePath);

                    // Check the length of the lines
                    if (oldLines.Length >= 20)
                    {
                        // Now overwrite the file without the first 10 lines
                        File.WriteAllLines(newFilePath, oldLines.Skip(10).ToArray());
                    }
                }

                using (StreamWriter sw = new StreamWriter(newFilePath, true))
                {
                    sw.WriteLine($"[{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}] : {message}");

                    sw.Flush();
                    sw.Close();
                }
            }
            catch
            { }
        }
    }
}
