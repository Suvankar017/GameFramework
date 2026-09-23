using System;
using System.IO;
using GameFramework.Runtime.Persistence;
using NUnit.Framework;

namespace GameFramework.Runtime.Tests.Persistence
{
    /// <summary>Phase 19: atomic replace, recovery from an interrupted rename-aside replace, and key
    /// validation. Uses real files under a unique temp directory.</summary>
    public class FilePersistenceStorageAtomicityTests
    {
        private string _tempRoot;
        private FilePersistenceStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "GameFrameworkAtomicTests_" + Guid.NewGuid().ToString("N"));
            _storage = new FilePersistenceStorage(_tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }

        private string PathFor(string key) => Path.Combine(_tempRoot, key + ".sav");

        [Test]
        public void WriteText_OverExistingFile_LeavesNoTempOrOldFiles()
        {
            _storage.WriteText("key", "first");
            _storage.WriteText("key", "second");

            Assert.AreEqual("second", _storage.ReadText("key"));
            Assert.IsFalse(File.Exists(PathFor("key") + ".tmp"));
            Assert.IsFalse(File.Exists(PathFor("key") + ".old"));
        }

        [Test]
        public void WriteText_RoundTripsNonAsciiText()
        {
            const string text = "Grüße — 日本語 — emoji 🎮";

            _storage.WriteText("key", text);

            Assert.AreEqual(text, _storage.ReadText("key"));
        }

        [Test]
        public void ReadText_CrashAfterPrimaryMovedAside_RestoresPreviousCopy()
        {
            // Simulates a process kill between "primary -> .old" and "temp -> primary".
            Directory.CreateDirectory(_tempRoot);
            File.WriteAllText(PathFor("key") + ".old", "last good");
            File.WriteAllText(PathFor("key") + ".tmp", "half-written");

            Assert.IsTrue(_storage.Exists("key"));
            Assert.AreEqual("last good", _storage.ReadText("key"));
            Assert.IsFalse(File.Exists(PathFor("key") + ".old"));
        }

        [Test]
        public void Exists_CrashAfterSwapBeforeCleanup_DiscardsStaleOldCopy()
        {
            Directory.CreateDirectory(_tempRoot);
            File.WriteAllText(PathFor("key"), "new");
            File.WriteAllText(PathFor("key") + ".old", "previous");

            Assert.IsTrue(_storage.Exists("key"));
            Assert.AreEqual("new", _storage.ReadText("key"));
            Assert.IsFalse(File.Exists(PathFor("key") + ".old"));
        }

        [Test]
        public void WriteText_LeftoverTempFromEarlierCrash_IsOverwrittenNotRead()
        {
            _storage.WriteText("key", "good");
            File.WriteAllText(PathFor("key") + ".tmp", "garbage from a killed process");

            Assert.AreEqual("good", _storage.ReadText("key"));

            _storage.WriteText("key", "newer");
            Assert.AreEqual("newer", _storage.ReadText("key"));
            Assert.IsFalse(File.Exists(PathFor("key") + ".tmp"));
        }

        [Test]
        public void Delete_RemovesCompanionFiles()
        {
            Directory.CreateDirectory(_tempRoot);
            File.WriteAllText(PathFor("key"), "x");
            File.WriteAllText(PathFor("key") + ".tmp", "x");
            File.WriteAllText(PathFor("key") + ".old", "x");

            _storage.Delete("key");

            Assert.IsFalse(File.Exists(PathFor("key")));
            Assert.IsFalse(File.Exists(PathFor("key") + ".tmp"));
            Assert.IsFalse(File.Exists(PathFor("key") + ".old"));
        }

        [TestCase("../escape")]
        [TestCase("sub/dir")]
        [TestCase("back\\slash")]
        [TestCase("drive:colon")]
        [TestCase("..")]
        [TestCase("tab\tchar")]
        public void WriteText_UnsafeKey_Throws(string key)
        {
            Assert.Throws<ArgumentException>(() => _storage.WriteText(key, "x"));
        }

        [Test]
        public void WriteText_OverlongKey_Throws()
        {
            Assert.Throws<ArgumentException>(() => _storage.WriteText(new string('k', FilePersistenceStorage.MaxKeyLength + 1), "x"));
        }

        [Test]
        public void ValidateKey_FrameworkStyleDottedKey_IsAccepted()
        {
            Assert.DoesNotThrow(() => FilePersistenceStorage.ValidateKey("GameFramework.PlayerData.default.Progression.bak"));
        }

        [Test]
        public void InMemoryStorage_AppliesSameKeyRules()
        {
            var memory = new InMemoryPersistenceStorage();

            Assert.Throws<ArgumentException>(() => memory.WriteText("a/b", "x"));
        }
    }
}
