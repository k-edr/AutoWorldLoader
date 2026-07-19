using System;
using System.IO;
using NUnit.Framework;

namespace AutoWorldLoader.Tests
{
    // ─────────────────────────────────────────────────────────────────────────
    // P2 — WorldLoader argument-guard tests (no game process required)
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class WorldLoaderGuardTests
    {
        // ── LaunchFromTemplate(string, string) ────────────────────────────────

        [TestCase(null)]
        [TestCase("")]
        public void LaunchFromTemplate_NullOrEmptyTemplatePath_ThrowsArgumentException(string path)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                WorldLoader.LaunchFromTemplate(path, "SomeName"));
            Assert.That(ex.ParamName, Is.EqualTo("templatePath"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void LaunchFromTemplate_NullOrEmptyTargetName_ThrowsArgumentException(string name)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                WorldLoader.LaunchFromTemplate("C:\\SomePath", name));
            Assert.That(ex.ParamName, Is.EqualTo("targetName"));
        }

        [Test]
        public void LaunchFromTemplate_NonExistentTemplateDir_ReturnsFalse()
        {
            var nonExistent = Path.Combine(
                Path.GetTempPath(),
                $"AWL_Guard_{Guid.NewGuid():N}",
                "NoSuchTemplate");

            var result = WorldLoader.LaunchFromTemplate(nonExistent, "MyWorld");
            Assert.That(result, Is.False);
        }

        // ── LoadByName() ──────────────────────────────────────────────────────

        [TestCase(null)]
        [TestCase("")]
        public void LoadByName_NullOrEmptyWorldName_ThrowsArgumentException(string name)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                WorldLoader.LoadByName(name));
            Assert.That(ex.ParamName, Is.EqualTo("worldName"));
        }

        // ── LoadByPath() ──────────────────────────────────────────────────────

        [TestCase(null)]
        [TestCase("")]
        public void LoadByPath_NullOrEmptySavePath_ThrowsArgumentException(string path)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                WorldLoader.LoadByPath(path));
            Assert.That(ex.ParamName, Is.EqualTo("savePath"));
        }

        [Test]
        public void LoadByPath_NonExistentDirectory_ThrowsDirectoryNotFoundException()
        {
            var nonExistent = Path.Combine(
                Path.GetTempPath(),
                $"AWL_Guard_{Guid.NewGuid():N}",
                "DoesNotExist");

            Assert.Throws<DirectoryNotFoundException>(() =>
                WorldLoader.LoadByPath(nonExistent));
        }
    }
}
