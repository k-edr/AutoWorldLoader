using System;
using System.IO;
using System.Reflection;

namespace AutoWorldLoader
{
    /// <summary>
    /// Stateless service that loads a Space Engineers saved world by name or path.
    /// Use when you need to trigger world loading programmatically from any context.
    /// </summary>
    public static class WorldLoader
    {
        private static readonly BindingFlags Flags =
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        /// <summary>
        /// Load a world by its save folder name.
        /// Resolves the save path under %APPDATA%\SpaceEngineers\Saves\&lt;SteamID&gt;\&lt;worldName&gt;.
        /// </summary>
        /// <param name="worldName">Name of the save folder.</param>
        /// <param name="isCustomPath">
        /// If false (default), resolves worldName through the standard Saves\SteamID folder.
        /// If true, treats worldName as a direct filesystem path — use for saves in non-standard locations.
        /// </param>
        /// <returns>True if the load was attempted; false if the save was not found.</returns>
        public static bool LoadByName(string worldName, bool isCustomPath = false)
        {
            if (string.IsNullOrEmpty(worldName))
                throw new ArgumentException("worldName must not be null or empty", nameof(worldName));

            var savePath = isCustomPath ? worldName : ResolveSavePath(worldName);
            if (savePath == null)
                return false;

            LoadByPath(savePath);
            return true;
        }

        /// <summary>
        /// Load a world by its full save path.
        /// </summary>
        public static void LoadByPath(string savePath)
        {
            if (string.IsNullOrEmpty(savePath))
                throw new ArgumentException("savePath must not be null or empty", nameof(savePath));

            if (!Directory.Exists(savePath))
                throw new DirectoryNotFoundException($"Save directory not found: {savePath}");

            var loaderType = Type.GetType(PluginConsts.SessionLoaderType);
            if (loaderType == null)
                throw new InvalidOperationException("MySessionLoader type not found — is the game running?");

            var method = loaderType.GetMethod(PluginConsts.LoadSessionByPathMethod, Flags)
                ?? loaderType.GetMethod(PluginConsts.LoadLastSessionMethod, Flags);

            if (method == null)
                throw new InvalidOperationException("No suitable session-loading method found");

            PluginLog.Info($"WorldLoader: calling {method.Name} for {savePath}");
            method.Invoke(null, method.GetParameters().Length == 0 ? null : new object[] { savePath });
        }

        /// <summary>
        /// Resolves a world name to its full save path.
        /// Returns null if the save or Steam ID folder cannot be found.
        /// </summary>
        public static string ResolveSavePath(string worldName)
        {
            var savesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                PluginConsts.AppDataFolder, PluginConsts.SavesFolder);

            var steamIdFolder = FindSteamIdFolder(savesDir);
            if (steamIdFolder == null)
                return null;

            var savePath = Path.Combine(steamIdFolder, worldName);
            return Directory.Exists(savePath) ? savePath : null;
        }

        private static string FindSteamIdFolder(string savesDir)
        {
            try
            {
                if (!Directory.Exists(savesDir))
                    return null;

                var dirs = Directory.GetDirectories(savesDir);
                if (dirs.Length != 1)
                {
                    PluginLog.Info($"WorldLoader: expected 1 folder under Saves, found {dirs.Length}");
                    return null;
                }

                return dirs[0];
            }
            catch (Exception ex)
            {
                PluginLog.Info($"WorldLoader: error searching Steam ID folder — {ex.Message}");
                return null;
            }
        }
    }
}
