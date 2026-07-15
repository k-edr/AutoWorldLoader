using System;

namespace AutoWorldLoader
{
    /// <summary>
    /// Shared constants for the plugin.
    /// </summary>
    internal static class PluginConsts
    {
        public const string AppDataFolder = "SpaceEngineers";
        public const string ConfigFileName = "AutoWorldLoader.json";
        public const string LogFileName = "AutoWorldLoader.log";
        public const string SavesFolder = "Saves";

        public const string SessionLoaderType = "Sandbox.Game.World.MySessionLoader, Sandbox.Game";
        public const string SessionType = "Sandbox.Game.World.MySession, Sandbox.Game";

        public const string LoadSessionByPathMethod = "LoadSessionByPath";
        public const string LoadLastSessionMethod = "LoadLastSession";

        public const string StaticProp = "Static";

        /// <summary>Seconds to wait before first load attempt.</summary>
        public const double InitialDelaySec = 3.0;

        /// <summary>Seconds between retry attempts.</summary>
        public const double RetryIntervalSec = 1.0;
    }
}
