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
        private string _worldName;
        private DateTime _initTime;
        private DateTime _lastCheck;

        public void Init(object gameInstance)
        {
            try
            {
                PluginLog.Info("=== AutoWorldLoader Init ===");

                _worldName = ReadWorldName();
                if (string.IsNullOrEmpty(_worldName))
                {
                    PluginLog.Info("worldName not configured — disabled");
                    return;
                }

                var savePath = WorldLoader.ResolveSavePath(_worldName);
                if (savePath == null)
                {
                    PluginLog.Error($"Save not found for world: {_worldName}");
                    return;
                }

                _initTime = DateTime.UtcNow;
                _lastCheck = _initTime;

                PluginLog.Info($"Ready. World: {_worldName} at {savePath}");
            }
            catch (Exception ex)
            {
                PluginLog.Error("Init error", ex);
            }
        }

        public void Update()
        {
            if (_loaded || string.IsNullOrEmpty(_worldName))
                return;

            var now = DateTime.UtcNow;
            var elapsed = (now - _initTime).TotalSeconds;
            if (elapsed < PluginConsts.InitialDelaySec)
                return;

            if ((now - _lastCheck).TotalSeconds < PluginConsts.RetryIntervalSec)
                return;

            _lastCheck = now;

            try
            {
                if (IsAtMainMenu())
                {
                    PluginLog.Info($"Loading world: {_worldName}");
                    WorldLoader.LoadByName(_worldName);
                    _loaded = true;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error("Update error", ex);
                _loaded = true;
            }
        }

        public void Dispose() { }

        private static bool IsAtMainMenu()
        {
            var sessionType = Type.GetType(PluginConsts.SessionType);
            if (sessionType == null) return false;

            var staticProp = sessionType.GetProperty(
                PluginConsts.StaticProp,
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            if (staticProp == null) return false;
            return staticProp.GetValue(null) == null;
        }

        private static string ReadWorldName()
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                PluginConsts.AppDataFolder, PluginConsts.ConfigFileName);

            if (!File.Exists(configPath))
            {
                PluginLog.Info("AutoWorldLoader.json not found");
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
                PluginLog.Error("Config read error", ex);
                return null;
            }
        }
    }
}
