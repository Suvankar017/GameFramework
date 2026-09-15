using System;
using System.IO;
using System.Linq;
using GameFramework.Runtime.Persistence;
using NUnit.Framework;

namespace GameFramework.Runtime.Tests.Persistence
{
    public class FilePersistenceStorageTests
    {
        private string _tempRoot;
        private FilePersistenceStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "GameFrameworkTests_" + Guid.NewGuid().ToString("N"));
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

        [Test]
        public void WriteThenRead_RoundTripsText()
        {
            _storage.WriteText("key", "hello world");

            Assert.AreEqual("hello world", _storage.ReadText("key"));
        }

        [Test]
        public void Exists_BeforeWrite_ReturnsFalse()
        {
            Assert.IsFalse(_storage.Exists("key"));
        }

        [Test]
        public void Exists_AfterWrite_ReturnsTrue()
        {
            _storage.WriteText("key", "x");

            Assert.IsTrue(_storage.Exists("key"));
        }

        [Test]
        public void ReadText_MissingKey_ReturnsNull()
        {
            Assert.IsNull(_storage.ReadText("missing"));
        }

        [Test]
        public void WriteText_OverwritesExistingContent()
        {
            _storage.WriteText("key", "first");

            _storage.WriteText("key", "second");

            Assert.AreEqual("second", _storage.ReadText("key"));
        }

        [Test]
        public void WriteText_DoesNotLeaveTempFileBehind()
        {
            _storage.WriteText("key", "content");

            bool anyTempFiles = Directory.GetFiles(_tempRoot).Any(f => f.EndsWith(".tmp"));

            Assert.IsFalse(anyTempFiles);
        }

        [Test]
        public void Delete_RemovesFile()
        {
            _storage.WriteText("key", "x");

            _storage.Delete("key");

            Assert.IsFalse(_storage.Exists("key"));
        }

        [Test]
        public void Delete_MissingKey_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _storage.Delete("missing"));
        }

        [Test]
        public void WriteText_CreatesRootDirectoryIfMissing()
        {
            Assert.IsFalse(Directory.Exists(_tempRoot));

            _storage.WriteText("key", "x");

            Assert.IsTrue(Directory.Exists(_tempRoot));
        }
    }
}
