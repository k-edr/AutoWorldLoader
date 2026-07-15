using System;
using System.Collections.Generic;

namespace AutoWorldLoader
{
    /// <summary>
    /// Maps <see cref="WorldTemplate"/> keys to concrete <see cref="IWorldTemplate"/>
    /// implementations via a dictionary. The last registration for a key wins.
    ///
    /// Built-in templates are registered in the static initializer.
    /// External code (e.g. other plugins) can call <see cref="Register"/>
    /// to add templates at runtime without modifying this file.
    ///
    /// Example:
    /// <code>
    /// WorldTemplateRegistry.Register(
    ///     WorldTemplate.MyCustomWorld,
    ///     new MyCustomWorldImpl());
    /// </code>
    /// </summary>
    public static class WorldTemplateRegistry
    {
        private static readonly Dictionary<WorldTemplate, IWorldTemplate> _templates
            = new()
            {
                [WorldTemplate.EmptyWorld_NoLimits_WithPB_NoMods] = new EmptyWorldTemplateImpl()
            };

        /// <summary>
        /// Register a template implementation at runtime.
        /// Overwrites any existing registration for the same key.
        /// </summary>
        public static void Register(WorldTemplate key, IWorldTemplate impl)
                {
                    if (key == WorldTemplate.None)
                        throw new ArgumentException("Cannot register an implementation for WorldTemplate.None.", nameof(key));

                    impl = impl ?? throw new ArgumentNullException(nameof(impl));

                    var overwritten = _templates.ContainsKey(key)
                        ? $" (overwrote {_templates[key].GetType().Name})"
                        : "";

                    _templates[key] = impl;
                    PluginLog.Info(
                        $"WorldTemplateRegistry: registered {impl.GetType().Name} for {key}{overwritten}");
                }

        /// <summary>
        /// Returns the implementation for the given template key.
        /// Throws if no implementation is registered.
        /// </summary>
        public static IWorldTemplate Get(WorldTemplate key)
        {
            if (_templates.TryGetValue(key, out var impl))
                return impl;

            throw new ArgumentOutOfRangeException(
                nameof(key), key,
                key == WorldTemplate.None
                    ? "WorldTemplate.None has no implementation — specify a concrete template."
                    : $"No implementation registered for {key}. Register it via WorldTemplateRegistry.Register().");
        }
    }
}
