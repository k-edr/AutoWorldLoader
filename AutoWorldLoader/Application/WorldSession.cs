using System;

namespace AutoWorldLoader
{
    /// <summary>
    /// Represents a launched world session. Disposing this object
    /// deletes the world from Saves — use for temporary test worlds
    /// created from templates.
    ///
    /// <code>
    /// using (var session = WorldLoader.CreateSession(WorldTemplate.EmptyWorld_NoLimits_WithPB_NoMods, "Test_001"))
    /// {
    ///     // world is loaded, do your tests
    /// } // world is deleted here
    /// </code>
    /// </summary>
    public sealed class WorldSession : IDisposable
    {
        /// <summary>Name of the world under Saves.</summary>
        public string WorldName { get; }

        private bool _disposed;

        internal WorldSession(string worldName)
        {
            WorldName = worldName ?? throw new ArgumentNullException(nameof(worldName));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                PluginLog.Info($"WorldSession: disposing — cleaning up {WorldName}");
                WorldLoader.Cleanup(WorldName);
            }
            catch (Exception ex)
            {
                PluginLog.Error($"WorldSession: cleanup failed for {WorldName}", ex);
            }
        }
    }
}
