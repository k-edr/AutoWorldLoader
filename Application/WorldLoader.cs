using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace AutoWorldLoader
{
    /// <summary>
    /// Stateless service that loads, copies, and cleans up Space Engineers saved worlds.
    /// </summary>
    public static class WorldLoader
    {
        private static readonly BindingFlags Flags =
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        // ── Session factory ───────────────────────────────────────────

        public static WorldSession CreateSession(WorldTemplate template, string targetName = null)
        {
            var name = targetName ?? TemplateFolder(template);
            LaunchFromTemplate(template, name);
            return new WorldSession(name);
        }

        public static WorldSession CreateSession(string templatePath, string targetName)
        {
            LaunchFromTemplate(templatePath, targetName);
            return new WorldSession(targetName);
        }

        // ── Template helpers ──────────────────────────────────────────

        public static string ResolveTemplatePath(WorldTemplate template)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                PluginConsts.AppDataFolder, PluginConsts.PluginFolder,
                PluginConsts.TemplatesFolder, TemplateFolder(template));
        }

        public static bool LaunchFromTemplate(WorldTemplate template, string targetName = null)
        {
            return LaunchFromTemplate(
                ResolveTemplatePath(template),
                targetName ?? TemplateFolder(template));
        }

        public static bool LaunchFromTemplate(string templatePath, string targetName)
        {
            if (string.IsNullOrEmpty(templatePath))
                throw new ArgumentException("templatePath must not be null or empty", nameof(templatePath));
            if (string.IsNullOrEmpty(targetName))
                throw new ArgumentException("targetName must not be null or empty", nameof(targetName));

            if (!Directory.Exists(templatePath))
            {
                PluginLog.Error($"Template not found: {templatePath}");
                return false;
            }

            if (!TryResolveSavesRoot(out var savesRoot))
                return false;

            var destPath = Path.Combine(savesRoot, targetName);

            PluginLog.Info($"WorldLoader: copying template {templatePath} → {destPath}");

            if (Directory.Exists(destPath))
            {
                PluginLog.Info($"WorldLoader: removing existing save at {destPath}");
                Directory.Delete(destPath, true);
            }

            CopyDirectory(templatePath, destPath);
            FixSessionName(destPath, targetName);

            PluginLog.Info($"WorldLoader: launching {targetName}");
            LoadByPath(destPath);
            return true;
        }

        /// <summary>
        /// Deletes a world from Saves by name. No-op if the save is not found.
        /// </summary>
        public static void Cleanup(string targetName)
        {
            if (!TryResolveSavePath(targetName, out var savePath))
            {
                PluginLog.Info($"WorldLoader: Cleanup — save not found: {targetName}");
                return;
            }

            PluginLog.Info($"WorldLoader: Cleanup — deleting {savePath}");
            Directory.Delete(savePath, true);
        }

        // ── Load ──────────────────────────────────────────────────────

        public static bool LoadByName(string worldName, bool isCustomPath = false)
        {
            if (string.IsNullOrEmpty(worldName))
                throw new ArgumentException("worldName must not be null or empty", nameof(worldName));

            var savePath = isCustomPath ? worldName : null;

            if (!isCustomPath && !TryResolveSavePath(worldName, out savePath))
                return false;

            LoadByPath(savePath);
            return true;
        }

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

        public static bool TryResolveSavePath(string worldName, out string savePath)
        {
            savePath = null;

            if (!TryResolveSavesRoot(out var savesRoot))
                return false;

            savePath = Path.Combine(savesRoot, worldName);
            if (!Directory.Exists(savePath))
            {
                PluginLog.Info($"WorldLoader: save not found: {savePath}");
                savePath = null;
                return false;
            }

            return true;
        }

        // ── Internals ─────────────────────────────────────────────────

        private static string TemplateFolder(WorldTemplate t) =>
            WorldTemplateRegistry.Get(t).FolderName;

        private static bool TryResolveSavesRoot(out string savesRoot)
        {
            savesRoot = null;

            var savesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                PluginConsts.AppDataFolder, PluginConsts.SavesFolder);

            return TryFindSteamIdFolder(savesDir, out savesRoot);
        }

        private static bool TryFindSteamIdFolder(string savesDir, out string steamIdFolder)
        {
            steamIdFolder = null;

            try
            {
                if (!Directory.Exists(savesDir))
                {
                    PluginLog.Info($"WorldLoader: Saves directory not found: {savesDir}");
                    return false;
                }

                var dirs = Directory.GetDirectories(savesDir);
                if (dirs.Length != 1)
                {
                    PluginLog.Info($"WorldLoader: expected 1 folder under Saves, found {dirs.Length}");
                    return false;
                }

                steamIdFolder = dirs[0];
                return true;
            }
            catch (Exception ex)
            {
                PluginLog.Info($"WorldLoader: error searching Steam ID folder — {ex.Message}");
                return false;
            }
        }

        private static void FixSessionName(string savePath, string newName)
        {
            foreach (var fileName in new[] { PluginConsts.SandboxSbcFile, PluginConsts.SandboxConfigSbcFile })
            {
                var filePath = Path.Combine(savePath, fileName);
                if (!File.Exists(filePath)) continue;

                try
                {
                    var doc = new XmlDocument();
                    doc.Load(filePath);

                    var nodes = doc.GetElementsByTagName("SessionName");
                    if (nodes.Count > 0)
                    {
                        nodes[0].InnerText = newName;
                        doc.Save(filePath);
                        PluginLog.Info($"WorldLoader: updated SessionName in {fileName} → {newName}");
                        continue;
                    }

                    nodes = doc.GetElementsByTagName("WorldName");
                    if (nodes.Count > 0)
                    {
                        nodes[0].InnerText = newName;
                        doc.Save(filePath);
                        PluginLog.Info($"WorldLoader: updated WorldName in {fileName} → {newName}");
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"WorldLoader: failed to update {fileName}", ex);
                }
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            try
            {
                Directory.CreateDirectory(destDir);

                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    var destFile = Path.Combine(destDir, Path.GetFileName(file));
                    File.Copy(file, destFile, overwrite: true);
                }

                foreach (var dir in Directory.GetDirectories(sourceDir))
                {
                    var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                    CopyDirectory(dir, destSubDir);
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"WorldLoader: CopyDirectory failed ({sourceDir} → {destDir})", ex);
                throw new IOException(
                    $"Failed to copy template from '{sourceDir}' to '{destDir}'. " +
                    "Check file permissions and disk space.", ex);
            }
        }
    }
}
