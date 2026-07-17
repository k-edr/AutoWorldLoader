namespace AutoWorldLoader
{
    /// <summary>
    /// Parsed configuration from AutoWorldLoader.json.
    /// </summary>
    internal sealed class PluginConfig
    {
        /// <summary>Name of an existing save (direct mode).</summary>
        public string WorldName;

        /// <summary>Template to copy and launch (template mode).</summary>
        public WorldTemplate? Template;

        /// <summary>Target folder name for the template copy.</summary>
        public string TargetName;

        /// <summary>
        /// If true, the world copy is deleted when the plugin unloads.
        /// Defaults to false — copies persist across sessions unless
        /// you explicitly opt into cleanup. Set to true for ephemeral test runs.
        /// </summary>
        public bool CleanupOnDispose;
    }
}
