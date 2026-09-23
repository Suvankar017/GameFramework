using System;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Runtime.Tests.Persistence
{
    /// <summary>Phase 19 save/load pipeline hardening: checksum integrity, version rejection,
    /// migration-chain safety, and the structured <see cref="PersistenceLoadStatus"/> result.</summary>
    public class PersistenceIntegrityTests
    {
        [Serializable]
        private class TestData
        {
            public int Value;
        }

        [Serializable]
        private class RawEnvelope
        {
            public int Version;
            public string Payload;
            public string Checksum;
        }

        private sealed class DelegateMigration : ISaveMigration
        {
            private readonly Func<string, string> _migrate;

            public DelegateMigration(int from, int to, Func<string, string> migrate)
            {
                FromVersion = from;
                ToVersion = to;
                _migrate = migrate;
            }

            public int FromVersion { get; }
            public int ToVersion { get; }
            public string Migrate(string payload) => _migrate(payload);
        }

        private InMemoryPersistenceStorage _storage;
        private PersistenceService _service;

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemoryPersistenceStorage();
            _service = new PersistenceService(_storage, new JsonPersistenceSerializer());
            _service.Initialize(new ServiceRegistry());
        }

        [Test]
        public void Save_WritesChecksumIntoEnvelope()
        {
            _service.Save("key", new TestData { Value = 1 }, 1);

            RawEnvelope envelope = JsonUtility.FromJson<RawEnvelope>(_storage.ReadText("key"));

            Assert.IsFalse(string.IsNullOrEmpty(envelope.Checksum));
            Assert.AreEqual(64, envelope.Checksum.Length, "SHA-256 hex is 64 characters.");
        }

        [Test]
        public void TryLoad_ValidSave_ReturnsLoaded()
        {
            _service.Save("key", new TestData { Value = 42 }, 1);

            PersistenceLoadStatus status = _service.TryLoad("key", 1, out TestData data);

            Assert.AreEqual(PersistenceLoadStatus.Loaded, status);
            Assert.AreEqual(42, data.Value);
        }

        [Test]
        public void TryLoad_MissingKey_ReturnsMissingWithoutCorruptMarker()
        {
            PersistenceLoadStatus status = _service.TryLoad("missing", 1, out TestData data);

            Assert.AreEqual(PersistenceLoadStatus.Missing, status);
            Assert.IsNull(data);
            Assert.IsFalse(_storage.Exists("missing.corrupt"));
        }

        [Test]
        public void TryLoad_PayloadModifiedButStillValidJson_DetectedAsCorrupted()
        {
            _service.Save("key", new TestData { Value = 10 }, 1);
            RawEnvelope envelope = JsonUtility.FromJson<RawEnvelope>(_storage.ReadText("key"));
            envelope.Payload = JsonUtility.ToJson(new TestData { Value = 999999 });
            _storage.WriteText("key", JsonUtility.ToJson(envelope));

            PersistenceLoadStatus status = _service.TryLoad("key", 1, out TestData data);

            Assert.AreEqual(PersistenceLoadStatus.Corrupted, status);
            Assert.IsNull(data);
            Assert.IsTrue(_storage.Exists("key.corrupt"), "Raw data must be preserved before any fallback.");
        }

        [Test]
        public void TryLoad_TruncatedFile_ReturnsCorrupted()
        {
            _service.Save("key", new TestData { Value = 10 }, 1);
            string raw = _storage.ReadText("key");
            _storage.WriteText("key", raw.Substring(0, raw.Length / 2));

            Assert.AreEqual(PersistenceLoadStatus.Corrupted, _service.TryLoad("key", 1, out TestData _));
        }

        [Test]
        public void TryLoad_EmptyEnvelope_ReturnsCorrupted()
        {
            _storage.WriteText("key", "{}");

            Assert.AreEqual(PersistenceLoadStatus.Corrupted, _service.TryLoad("key", 1, out TestData _));
        }

        [Test]
        public void TryLoad_LegacyEnvelopeWithoutChecksum_StillLoads()
        {
            var legacy = new RawEnvelope { Version = 1, Payload = JsonUtility.ToJson(new TestData { Value = 5 }) };
            _storage.WriteText("key", JsonUtility.ToJson(legacy));

            PersistenceLoadStatus status = _service.TryLoad("key", 1, out TestData data);

            Assert.AreEqual(PersistenceLoadStatus.Loaded, status);
            Assert.AreEqual(5, data.Value);
        }

        [Test]
        public void TryLoad_FutureVersion_RejectedAsUnsupportedAndPreserved()
        {
            _service.Save("key", new TestData { Value = 7 }, 3);

            PersistenceLoadStatus status = _service.TryLoad("key", 2, out TestData data);

            Assert.AreEqual(PersistenceLoadStatus.UnsupportedVersion, status);
            Assert.IsNull(data);
            Assert.IsTrue(_storage.Exists("key.corrupt"));
            Assert.IsTrue(_storage.Exists("key"), "The newer save itself must not be deleted.");
        }

        [Test]
        public void Load_FutureVersion_ReturnsDefault()
        {
            _service.Save("key", new TestData { Value = 7 }, 3);
            var fallback = new TestData { Value = -1 };

            Assert.AreSame(fallback, _service.Load("key", 2, fallback));
        }

        [Test]
        public void TryLoad_OldVersionWithMigration_ReturnsMigrated()
        {
            _service.Save("key", new TestData { Value = 2 }, 1);
            _service.RegisterMigration("key", new DelegateMigration(1, 2, payload => payload));

            PersistenceLoadStatus status = _service.TryLoad("key", 2, out TestData data);

            Assert.AreEqual(PersistenceLoadStatus.Migrated, status);
            Assert.AreEqual(2, data.Value);
        }

        [Test]
        public void TryLoad_NoMigrationRegistered_ReturnsMigrationMissingAndPreservesData()
        {
            _service.Save("key", new TestData { Value = 2 }, 1);

            Assert.AreEqual(PersistenceLoadStatus.MigrationMissing, _service.TryLoad("key", 2, out TestData _));
            Assert.IsTrue(_storage.Exists("key.corrupt"));
        }

        [Test]
        public void TryLoad_MigrationThrows_ReturnsMigrationFailedAndKeepsOriginal()
        {
            _service.Save("key", new TestData { Value = 2 }, 1);
            string original = _storage.ReadText("key");
            _service.RegisterMigration("key", new DelegateMigration(1, 2, _ => throw new InvalidOperationException("boom")));

            Assert.AreEqual(PersistenceLoadStatus.MigrationFailed, _service.TryLoad("key", 2, out TestData _));
            Assert.AreEqual(original, _storage.ReadText("key"), "A failed migration must not modify the stored original.");
        }

        [Test]
        public void TryLoad_MigrationReturnsNull_ReturnsMigrationFailed()
        {
            _service.Save("key", new TestData { Value = 2 }, 1);
            _service.RegisterMigration("key", new DelegateMigration(1, 2, _ => null));

            Assert.AreEqual(PersistenceLoadStatus.MigrationFailed, _service.TryLoad("key", 2, out TestData _));
        }

        [Test]
        public void TryLoad_NonAdvancingMigration_FailsInsteadOfLoopingForever()
        {
            _service.Save("key", new TestData { Value = 2 }, 1);
            _service.RegisterMigration("key", new DelegateMigration(1, 1, payload => payload));

            Assert.AreEqual(PersistenceLoadStatus.MigrationFailed, _service.TryLoad("key", 3, out TestData _));
        }

        [Test]
        public void TryLoad_MigrationOvershootingTarget_Fails()
        {
            _service.Save("key", new TestData { Value = 2 }, 1);
            _service.RegisterMigration("key", new DelegateMigration(1, 5, payload => payload));

            Assert.AreEqual(PersistenceLoadStatus.MigrationFailed, _service.TryLoad("key", 2, out TestData _));
        }

        [Test]
        public void TryLoad_NegativeStoredVersion_ReturnsCorrupted()
        {
            var envelope = new RawEnvelope { Version = -4, Payload = JsonUtility.ToJson(new TestData()) };
            _storage.WriteText("key", JsonUtility.ToJson(envelope));

            Assert.AreEqual(PersistenceLoadStatus.Corrupted, _service.TryLoad("key", 1, out TestData _));
        }

        [Test]
        public void Save_NullData_ThrowsAndLeavesExistingSaveIntact()
        {
            _service.Save("key", new TestData { Value = 3 }, 1);

            Assert.Throws<ArgumentNullException>(() => _service.Save<TestData>("key", null, 1));
            Assert.AreEqual(3, _service.Load("key", 1, new TestData()).Value);
        }

        [Test]
        public void TryLoad_StorageReadThrows_ReturnsUnreadable()
        {
            var service = new PersistenceService(new ThrowingReadStorage(), new JsonPersistenceSerializer());
            service.Initialize(new ServiceRegistry());

            Assert.AreEqual(PersistenceLoadStatus.Unreadable, service.TryLoad("key", 1, out TestData _));
        }

        [Test]
        public void StatusExtensions_ClassifyOutcomes()
        {
            Assert.IsTrue(PersistenceLoadStatus.Loaded.IsSuccess());
            Assert.IsTrue(PersistenceLoadStatus.Migrated.IsSuccess());
            Assert.IsFalse(PersistenceLoadStatus.Missing.IsSuccess());
            Assert.IsFalse(PersistenceLoadStatus.Missing.IsFailure());
            Assert.IsTrue(PersistenceLoadStatus.Corrupted.IsFailure());
            Assert.IsTrue(PersistenceLoadStatus.UnsupportedVersion.IsFailure());
        }

        private sealed class ThrowingReadStorage : IPersistenceStorage
        {
            public bool Exists(string key) => true;
            public string ReadText(string key) => throw new System.IO.IOException("Simulated read failure.");
            public void WriteText(string key, string contents) { }
            public void Delete(string key) { }
        }
    }
}
