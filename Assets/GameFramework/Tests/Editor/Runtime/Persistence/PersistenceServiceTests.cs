using System;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Runtime.Tests.Persistence
{
    public class PersistenceServiceTests
    {
        [Serializable]
        private class TestDataV1
        {
            public int Value;
        }

        [Serializable]
        private class TestDataV2
        {
            public int Value;
            public string NewField;
        }

        private sealed class TestMigrationV1ToV2 : ISaveMigration
        {
            public int FromVersion => 1;
            public int ToVersion => 2;

            public string Migrate(string payload)
            {
                var old = JsonUtility.FromJson<TestDataV1>(payload);
                var migrated = new TestDataV2 { Value = old.Value, NewField = "migrated" };
                return JsonUtility.ToJson(migrated);
            }
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
        public void Exists_NothingSaved_ReturnsFalse()
        {
            Assert.IsFalse(_service.Exists("key"));
        }

        [Test]
        public void Save_ThenExists_ReturnsTrue()
        {
            _service.Save("key", new TestDataV1 { Value = 1 }, 1);

            Assert.IsTrue(_service.Exists("key"));
        }

        [Test]
        public void SaveThenLoad_RoundTripsData()
        {
            _service.Save("key", new TestDataV1 { Value = 42 }, 1);

            TestDataV1 loaded = _service.Load("key", 1, new TestDataV1());

            Assert.AreEqual(42, loaded.Value);
        }

        [Test]
        public void Load_NoSaveExists_ReturnsDefaultValue()
        {
            var defaultValue = new TestDataV1 { Value = -1 };

            TestDataV1 loaded = _service.Load("missing", 1, defaultValue);

            Assert.AreSame(defaultValue, loaded);
        }

        [Test]
        public void Delete_RemovesSave()
        {
            _service.Save("key", new TestDataV1 { Value = 1 }, 1);

            _service.Delete("key");

            Assert.IsFalse(_service.Exists("key"));
        }

        [Test]
        public void Load_AfterDelete_ReturnsDefault()
        {
            _service.Save("key", new TestDataV1 { Value = 1 }, 1);
            _service.Delete("key");

            TestDataV1 loaded = _service.Load("key", 1, new TestDataV1 { Value = -1 });

            Assert.AreEqual(-1, loaded.Value);
        }

        [Test]
        public void Load_CorruptedData_ReturnsDefaultAndBacksUpRawData()
        {
            _storage.WriteText("key", "not valid json {{{");

            TestDataV1 loaded = _service.Load("key", 1, new TestDataV1 { Value = -1 });

            Assert.AreEqual(-1, loaded.Value);
            Assert.IsTrue(_storage.Exists("key.corrupt"));
        }

        [Test]
        public void Load_VersionAheadWithNoMigrationRegistered_ReturnsDefault()
        {
            _service.Save("key", new TestDataV1 { Value = 1 }, 1);

            TestDataV1 loaded = _service.Load("key", 2, new TestDataV1 { Value = -1 });

            Assert.AreEqual(-1, loaded.Value);
        }

        [Test]
        public void Load_WithRegisteredMigration_MigratesToCurrentVersion()
        {
            _service.Save("key", new TestDataV1 { Value = 7 }, 1);
            _service.RegisterMigration("key", new TestMigrationV1ToV2());

            TestDataV2 loaded = _service.Load("key", 2, new TestDataV2());

            Assert.AreEqual(7, loaded.Value);
            Assert.AreEqual("migrated", loaded.NewField);
        }

        [Test]
        public void RegisterMigration_DuplicateFromVersionForSameKey_Throws()
        {
            _service.RegisterMigration("key", new TestMigrationV1ToV2());

            Assert.Throws<InvalidOperationException>(
                () => _service.RegisterMigration("key", new TestMigrationV1ToV2()));
        }

        [Test]
        public void Migration_MigrateIsPureAndDeterministic()
        {
            var migration = new TestMigrationV1ToV2();
            string input = JsonUtility.ToJson(new TestDataV1 { Value = 5 });

            string first = migration.Migrate(input);
            string second = migration.Migrate(input);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void Save_RepeatedSaves_OverwritesPreviousData()
        {
            _service.Save("key", new TestDataV1 { Value = 1 }, 1);
            _service.Save("key", new TestDataV1 { Value = 2 }, 1);

            TestDataV1 loaded = _service.Load("key", 1, new TestDataV1());

            Assert.AreEqual(2, loaded.Value);
        }

        [Test]
        public void Load_RepeatedLoads_ReturnConsistentData()
        {
            _service.Save("key", new TestDataV1 { Value = 9 }, 1);

            TestDataV1 first = _service.Load("key", 1, new TestDataV1());
            TestDataV1 second = _service.Load("key", 1, new TestDataV1());

            Assert.AreEqual(first.Value, second.Value);
        }
    }
}
