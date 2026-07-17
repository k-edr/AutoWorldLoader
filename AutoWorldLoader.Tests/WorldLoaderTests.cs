using System;
using System.IO;
using NUnit.Framework;

namespace AutoWorldLoader.Tests
{
    [TestFixture]
    public class WorldLoaderTests
    {
        private string _tempRoot;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), $"AWL_Test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }

        [Test]
        public void TryFindSteamIdFolder_NonExistentDirectory_ReturnsFalse()
        {
            var nonExistent = Path.Combine(_tempRoot, "does_not_exist");
            bool result = WorldLoader.TryFindSteamIdFolder(nonExistent, out var folder);
            Assert.That(result, Is.False);
            Assert.That(folder, Is.Null);
        }

        [Test]
        public void TryFindSteamIdFolder_EmptyDirectory_ReturnsFalse()
        {
            bool result = WorldLoader.TryFindSteamIdFolder(_tempRoot, out var folder);
            Assert.That(result, Is.False);
            Assert.That(folder, Is.Null);
        }

        [Test]
        public void TryFindSteamIdFolder_OnlyCloudFolder_ReturnsFalse()
        {
            Directory.CreateDirectory(Path.Combine(_tempRoot, "Cloud"));
            bool result = WorldLoader.TryFindSteamIdFolder(_tempRoot, out var folder);
            Assert.That(result, Is.False);
            Assert.That(folder, Is.Null);
        }

        [Test]
        public void TryFindSteamIdFolder_SingleNumericFolder_ReturnsIt()
        {
            var steamId = "76561199017018740";
            Directory.CreateDirectory(Path.Combine(_tempRoot, steamId));
            bool result = WorldLoader.TryFindSteamIdFolder(_tempRoot, out var folder);
            Assert.That(result, Is.True);
            Assert.That(Path.GetFileName(folder), Is.EqualTo(steamId));
        }

        [Test]
        public void TryFindSteamIdFolder_MultipleNumericFolders_ReturnsMostRecent()
        {
            var older = Path.Combine(_tempRoot, "11111111111111111");
            var newer = Path.Combine(_tempRoot, "22222222222222222");
            Directory.CreateDirectory(older);
            System.Threading.Thread.Sleep(10);
            Directory.CreateDirectory(newer);
            bool result = WorldLoader.TryFindSteamIdFolder(_tempRoot, out var folder);
            Assert.That(result, Is.True);
            Assert.That(Path.GetFileName(folder), Is.EqualTo("22222222222222222"));
        }

        [Test]
        public void TryFindSteamIdFolder_SkipsNonNumericFolders()
        {
            Directory.CreateDirectory(Path.Combine(_tempRoot, "Cloud"));
            Directory.CreateDirectory(Path.Combine(_tempRoot, "SomeOtherFolder"));
            Directory.CreateDirectory(Path.Combine(_tempRoot, "76561199017018740"));
            bool result = WorldLoader.TryFindSteamIdFolder(_tempRoot, out var folder);
            Assert.That(result, Is.True);
            Assert.That(Path.GetFileName(folder), Is.EqualTo("76561199017018740"));
        }
    }
}
