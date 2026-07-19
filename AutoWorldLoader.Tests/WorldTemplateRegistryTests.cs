using System;
using System.IO;
using NUnit.Framework;

namespace AutoWorldLoader.Tests
{
    // ─────────────────────────────────────────────────────────────────────────
    // P1 — IWorldTemplate contract + WorldTemplateRegistry behaviour
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class WorldTemplateRegistryTests
    {
        // ── P1: built-in Get() returns correct types ──────────────────────────

        [Test]
        public void Get_EmptyWorldTemplate_ReturnsNonNullImpl()
        {
            var impl = WorldTemplateRegistry.Get(WorldTemplate.EmptyWorld_NoLimits_WithPB_NoMods);
            Assert.That(impl, Is.Not.Null);
        }

        [Test]
        public void Get_EmptyWorldTemplate_HasExpectedFolderName()
        {
            var impl = WorldTemplateRegistry.Get(WorldTemplate.EmptyWorld_NoLimits_WithPB_NoMods);
            Assert.That(impl.FolderName, Is.EqualTo("EmptyWorld_NoLimits_WithPB_NoMods"));
        }

        [Test]
        public void Get_StarSystemTemplate_ReturnsNonNullImpl()
        {
            var impl = WorldTemplateRegistry.Get(WorldTemplate.StarSystem_PbOn_NoAutosave_NoMods);
            Assert.That(impl, Is.Not.Null);
        }

        [Test]
        public void Get_StarSystemTemplate_HasExpectedFolderName()
        {
            var impl = WorldTemplateRegistry.Get(WorldTemplate.StarSystem_PbOn_NoAutosave_NoMods);
            Assert.That(impl.FolderName, Is.EqualTo("StarSystemTemplate_PbOn_NoAutosave_NoMods"));
        }

        // ── P1: Get() throws for None / unregistered ──────────────────────────

        [Test]
        public void Get_None_ThrowsArgumentOutOfRangeException()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldTemplateRegistry.Get(WorldTemplate.None));
            Assert.That(ex.ParamName, Is.EqualTo("key"));
        }

        [Test]
        public void Get_UnregisteredEnumValue_ThrowsArgumentOutOfRangeException()
        {
            // Cast an out-of-range integer to WorldTemplate to simulate an
            // unregistered enum value.
            var unregistered = (WorldTemplate)999;
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldTemplateRegistry.Get(unregistered));
            Assert.That(ex.ParamName, Is.EqualTo("key"));
        }

        // ── P1: Register() guard clauses ──────────────────────────────────────

        [Test]
        public void Register_NoneKey_ThrowsArgumentException()
        {
            var stub = new StubWorldTemplate("SomeFolder");
            var ex = Assert.Throws<ArgumentException>(() =>
                WorldTemplateRegistry.Register(WorldTemplate.None, stub));
            Assert.That(ex.ParamName, Is.EqualTo("key"));
        }

        [Test]
        public void Register_NullImpl_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                WorldTemplateRegistry.Register(WorldTemplate.EmptyWorld_NoLimits_WithPB_NoMods, null));
            Assert.That(ex.ParamName, Is.EqualTo("impl"));
        }

        // ── P1: Register() overwrites and Get() returns new impl ──────────────

        [Test]
        public void Register_OverwritesExistingKey_GetReturnsNewImpl()
        {
            var custom = new StubWorldTemplate("MyCustomFolder");
            WorldTemplateRegistry.Register(WorldTemplate.EmptyWorld_NoLimits_WithPB_NoMods, custom);

            var impl = WorldTemplateRegistry.Get(WorldTemplate.EmptyWorld_NoLimits_WithPB_NoMods);
            Assert.That(impl.FolderName, Is.EqualTo("MyCustomFolder"));

            // Restore the original so other tests are not affected.
            WorldTemplateRegistry.Register(
                WorldTemplate.EmptyWorld_NoLimits_WithPB_NoMods,
                new RestoreEmptyWorldTemplate());
        }

        [Test]
        public void Register_ThenGet_ReturnsRegisteredImpl()
        {
            var custom = new StubWorldTemplate("StarSystemCustom");
            WorldTemplateRegistry.Register(WorldTemplate.StarSystem_PbOn_NoAutosave_NoMods, custom);

            var impl = WorldTemplateRegistry.Get(WorldTemplate.StarSystem_PbOn_NoAutosave_NoMods);
            Assert.That(impl.FolderName, Is.EqualTo("StarSystemCustom"));

            // Restore original.
            WorldTemplateRegistry.Register(
                WorldTemplate.StarSystem_PbOn_NoAutosave_NoMods,
                new RestoreStarSystemTemplate());
        }

        // ── P1: IWorldTemplate.FolderName is never null/empty ─────────────────

        [TestCase(WorldTemplate.EmptyWorld_NoLimits_WithPB_NoMods)]
        [TestCase(WorldTemplate.StarSystem_PbOn_NoAutosave_NoMods)]
        public void FolderName_BuiltInImpl_IsNotNullOrEmpty(WorldTemplate key)
        {
            var impl = WorldTemplateRegistry.Get(key);
            Assert.That(impl.FolderName, Is.Not.Null.And.Not.Empty);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private sealed class StubWorldTemplate : IWorldTemplate
        {
            public string FolderName { get; }
            public StubWorldTemplate(string folder) => FolderName = folder;
        }

        // Restore helpers — must match the exact folder names from production code.
        private sealed class RestoreEmptyWorldTemplate : IWorldTemplate
        {
            public string FolderName => "EmptyWorld_NoLimits_WithPB_NoMods";
        }

        private sealed class RestoreStarSystemTemplate : IWorldTemplate
        {
            public string FolderName => "StarSystemTemplate_PbOn_NoAutosave_NoMods";
        }
    }
}
