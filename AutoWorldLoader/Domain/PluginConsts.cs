namespace AutoWorldLoader
{
    /// <summary>
    /// Shared constants for the plugin.
    /// </summary>
    internal static class PluginConsts
    {
        public const string AppDataFolder = "SpaceEngineers";
        public const string PluginFolder = "AutoWorldLoader";
        public const string ConfigFileName = "AutoWorldLoader.json";
        public const string LogFileName = "AutoWorldLoader.log";
        public const string SavesFolder = "Saves";
        public const string TemplatesFolder = "Templates";

        /// <summary>
        /// Assembly-qualified type name for MySessionLoader.
        /// Used via reflection to call LoadSessionByPath / LoadLastSession
        /// without a compile-time dependency on Sandbox.Game.
        /// </summary>
        public const string SessionLoaderType = "Sandbox.Game.World.MySessionLoader, Sandbox.Game";

        /// <summary>
        /// Assembly-qualified type name for MySession.
        /// Used via reflection to detect whether we are at the main menu
        /// (MySession.Static == null means no session is loaded).
        /// </summary>
        public const string SessionType = "Sandbox.Game.World.MySession, Sandbox.Game";

        public const string LoadSessionByPathMethod = "LoadSessionByPath";
        public const string LoadLastSessionMethod = "LoadLastSession";

        public const string StaticProp = "Static";

        public const string SandboxSbcFile = "Sandbox.sbc";
        public const string SandboxConfigSbcFile = "Sandbox_config.sbc";

        /// <summary>Max whitespace chars to skip during JSON parsing.</summary>
        public const int JsonMaxSkip = 512;

        /// <summary>Seconds to wait before first load attempt.</summary>
        public const double InitialDelaySec = 3.0;

        /// <summary>Seconds between retry attempts.</summary>
        public const double RetryIntervalSec = 1.0;
    }
}
