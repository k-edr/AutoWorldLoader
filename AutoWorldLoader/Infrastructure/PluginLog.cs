using System;
using System.IO;

namespace AutoWorldLoader
{
    /// <summary>
    /// Lightweight file logger. All output goes to %APPDATA%\SpaceEngineers\AutoWorldLoader.log.
    /// </summary>
    internal static class PluginLog
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            PluginConsts.AppDataFolder, PluginConsts.LogFileName);

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        public static void Error(string message, Exception ex)
        {
            Write("ERROR", $"{message}: {ex}");
        }

        private static void Write(string level, string message)
        {
            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch { }
        }
    }
}
