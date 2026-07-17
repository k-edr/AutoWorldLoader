using System;
using System.IO;

namespace AutoWorldLoader
{
    /// <summary>
    /// Parses AutoWorldLoader.json without external JSON dependency.
    /// </summary>
    internal static class PluginConfigReader
    {
        /// <summary>
        /// Reads and parses the config file. Returns null if the file is missing.
        /// </summary>
        public static PluginConfig Read()
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
                var cfg = new PluginConfig();

                cfg.WorldName   = ExtractString(json, "worldName");
                cfg.TargetName  = ExtractString(json, "targetName");

                var templateStr = ExtractString(json, "template");
                if (!string.IsNullOrEmpty(templateStr))
                {
                    if (Enum.TryParse<WorldTemplate>(templateStr, ignoreCase: true, out var tpl)
                        && tpl != WorldTemplate.None)
                    {
                        cfg.Template = tpl;
                    }
                    else
                    {
                        PluginLog.Error($"Unknown or invalid template: {templateStr}");
                    }
                }

                cfg.CleanupOnDispose = ExtractBool(json, "cleanupOnDispose");

                return cfg;
            }
            catch (Exception ex)
            {
                PluginLog.Error("Config read error", ex);
                return null;
            }
        }

        // ── Manual JSON helpers ───────────────────────────────────────

        private static string ExtractString(string json, string key)
        {
            var search = "\"" + key + "\"";
            var idx = json.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            var colonIdx = json.IndexOf(':', idx + search.Length);
            if (colonIdx < 0) return null;

            var valStart = colonIdx + 1;
            var skipLimit = Math.Min(valStart + PluginConsts.JsonMaxSkip, json.Length);
                        while (valStart < json.Length && char.IsWhiteSpace(json[valStart]))
                        {
                            if (valStart >= skipLimit)
                            {
                                PluginLog.Error(
                                    $"PluginConfigReader: exceeded JsonMaxSkip ({PluginConsts.JsonMaxSkip}) " +
                                    $"while parsing key '{key}' — JSON is likely malformed.");
                                return null;
                            }
                            valStart++;
                        }

            if (valStart >= json.Length) return null;

            if (json[valStart] == '"')
            {
                var closeQuote = json.IndexOf('"', valStart + 1);
                if (closeQuote < 0) return null;
                return json.Substring(valStart + 1, closeQuote - valStart - 1);
            }

            // Boolean / number — extract till comma, brace, or newline
            var end = json.IndexOfAny(new[] { ',', '}', '\r', '\n' }, valStart);
            if (end < 0) end = json.Length;
            return json.Substring(valStart, end - valStart).Trim();
        }

        private static bool ExtractBool(string json, string key)
        {
            var val = ExtractString(json, key);
            return val != null && val.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
