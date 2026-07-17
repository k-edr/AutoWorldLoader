using System;
using System.Reflection;
using VRage.Plugins;

namespace AutoWorldLoader
{
    /// <summary>
    /// Auto-loads a saved world when the game reaches the main menu.
    ///
    /// Two modes (configured via AutoWorldLoader.json):
    ///   1. Direct — specify "worldName" of an existing save.
    ///   2. Template — specify "template" enum name; a fresh copy is
    ///      created under Saves and loaded. Optionally cleaned up on Dispose.
    /// </summary>
    public class Plugin : IPlugin
    {
        private bool _loaded;
        private string _worldName;
        private bool _cleanupOnDispose;
        private DateTime _initTime;
        private DateTime _lastCheck;

        public void Init(object gameInstance)
        {
            try
            {
                PluginLog.Info("=== AutoWorldLoader Init ===");

                var config = PluginConfigReader.Read();
                if (config == null)
                {
                    PluginLog.Info("Config not found — disabled");
                    return;
                }

                if (config.Template != null)
                {
                    InitTemplateMode(config);
                    return;
                }

                InitDirectMode(config);
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

        public void Dispose()
        {
            if (_cleanupOnDispose && !string.IsNullOrEmpty(_worldName))
            {
                try
                {
                    PluginLog.Info($"Dispose: cleaning up {_worldName}");
                    WorldLoader.Cleanup(_worldName);
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Dispose: cleanup failed for {_worldName}", ex);
                }
            }
        }

        // ── Init helpers ──────────────────────────────────────────────

        private void InitTemplateMode(PluginConfig config)
        {
            var template = config.Template.Value;
            _worldName = config.TargetName ?? WorldTemplateRegistry.Get(template).FolderName;
            _cleanupOnDispose = config.CleanupOnDispose;

            PluginLog.Info($"Template mode: {template} → {_worldName}" +
                           (_cleanupOnDispose ? " (cleanup on Dispose)" : ""));

            WorldLoader.LaunchFromTemplate(template, _worldName);
            _loaded = true;
        }

        private void InitDirectMode(PluginConfig config)
        {
            _worldName = config.WorldName;
            if (string.IsNullOrEmpty(_worldName))
            {
                PluginLog.Info("Neither template nor worldName configured — disabled");
                return;
            }

            if (!WorldLoader.TryResolveSavePath(_worldName, out var savePath))
            {
                PluginLog.Error($"Save not found for world: {_worldName}");
                return;
            }

            _initTime = DateTime.UtcNow;
            _lastCheck = _initTime;

            PluginLog.Info($"Ready. World: {_worldName} at {savePath}");
        }

        // ── Game helpers ──────────────────────────────────────────────

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
    }
}
