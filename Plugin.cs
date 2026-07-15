using System;
using System.IO;
using System.Reflection;
using VRage.Plugins;

namespace AutoWorldLoader
{
    /// <summary>
    /// Auto-loads a saved world when the game reaches the main menu.
    /// Configure via %APPDATA%\SpaceEngineers\AutoWorldLoader.json
    /// </summary>
    public class Plugin : IPlugin
    {
        private bool _loaded;
        private int _frameCount;
        private string _worldName;
        private string _savePath;

        private static readonly BindingFlags Flags =
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        public void Init(object gameInstance)
        {
            try
            {
                Log("=== AutoWorldLoader Init ===");

                _worldName = ReadWorldName();
                if (string.IsNullOrEmpty(_worldName))
                {
                    Log("worldName not configured — disabled");
                    return;
                }

                var savesDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SpaceEngineers", "Saves");

                var savesRoot = FindSteamIdFolder(savesDir);
                if (savesRoot == null)
                {
                    Log("ERROR: No Steam ID folder found in Saves");
                    return;
                }

                _savePath = Path.Combine(savesRoot, _worldName);

                if (!Directory.Exists(_savePath))
                {
                    Log($"ERROR: Save not found: {_savePath}");
                    return;
                }

                Log($"Ready. World: {_worldName}");
            }
            catch (Exception ex)
            {
                Log($"Init error: {ex}");
            }
        }

        public void Update()
        {
            if (_loaded || string.IsNullOrEmpty(_savePath))
                return;

            _frameCount++;

            if (_frameCount < 180) return;
            if (_frameCount % 60 != 0) return;

            try
            {
                var atMainMenu = IsAtMainMenu();
                if (atMainMenu)
                {
                    Log($"Loading world: {_worldName}");
                    LoadWorld(_savePath);
                    _loaded = true;
                }
            }
            catch (Exception ex)
            {
                Log($"Update error: {ex}");
                _loaded = true;
            }
        }

        public void Dispose() { }

        private static bool IsAtMainMenu()
        {
            var sessionType = Type.GetType("Sandbox.Game.World.MySession, Sandbox.Game");
            if (sessionType == null) return false;

            var staticProp = sessionType.GetProperty("Static", Flags);
            if (staticProp == null) return false;

            return staticProp.GetValue(null) == null;
        }

        private static void LoadWorld(string savePath)
        {
            var loaderType = Type.GetType("Sandbox.Game.World.MySessionLoader, Sandbox.Game");
            if (loaderType == null)
            {
                Log("ERROR: MySessionLoader type not found");
                return;
            }

            var method = loaderType.GetMethod("LoadSessionByPath", Flags)
                ?? loaderType.GetMethod("LoadLastSession", Flags);

            if (method != null)
            {
                Log($"Calling {method.Name}");
                method.Invoke(null, method.GetParameters().Length == 0 ? null : new object[] { savePath });
            }
            else
            {
                Log("ERROR: No suitable method found");
            }
        }

        /// <summary>
        /// Finds the single numeric Steam ID folder under Saves.
        /// Returns null if zero or multiple folders exist.
        /// </summary>
        private static string FindSteamIdFolder(string savesDir)
        {
            try
            {
                if (!Directory.Exists(savesDir))
                    return null;

                var dirs = Directory.GetDirectories(savesDir);
                if (dirs.Length != 1)
                {
                    Log($"Expected 1 folder under Saves, found {dirs.Length}");
                    return null;
                }

                Log($"Found Steam ID folder: {Path.GetFileName(dirs[0])}");
                return dirs[0];
            }
            catch (Exception ex)
            {
                Log($"Error searching Steam ID folder: {ex}");
                return null;
            }
        }

        private static string ReadWorldName()
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpaceEngineers", "AutoWorldLoader.json");

            if (!File.Exists(configPath))
            {
                Log("AutoWorldLoader.json not found");
                return null;
            }

            try
            {
                var json = File.ReadAllText(configPath);
                var key = "\"worldName\"";
                var idx = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return null;

                var colonIdx = json.IndexOf(':', idx + key.Length);
                if (colonIdx < 0) return null;

                var openQuote = json.IndexOf('"', colonIdx + 1);
                if (openQuote < 0) return null;

                var closeQuote = json.IndexOf('"', openQuote + 1);
                if (closeQuote < 0) return null;

                return json.Substring(openQuote + 1, closeQuote - openQuote - 1);
            }
            catch (Exception ex)
            {
                Log($"Config read error: {ex}");
                return null;
            }
        }

        private static void Log(string msg)
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpaceEngineers", "AutoWorldLoader.log");

            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {msg}";
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch { }
        }
    }
}
