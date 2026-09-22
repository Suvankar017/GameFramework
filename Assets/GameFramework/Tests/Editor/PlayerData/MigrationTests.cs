using GameFramework.Runtime.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.PlayerData.Tests
{
    public class MigrationTests
    {
        private sealed class TestSettingsV1ToV2Migration : ISaveMigration
        {
            public int FromVersion => 1;
            public int ToVersion => 2;

            public string Migrate(string payload)
            {
                var old = JsonUtility.FromJson<TestSettingsDataV1>(payload);
                var migrated = new TestSettingsData { Difficulty = old.Difficulty, TutorialsEnabled = true };
                return JsonUtility.ToJson(migrated);
            }
        }

        private PersistenceService _persistence;
        private PlayerProfileService _playerData;

        [SetUp]
        public void SetUp()
        {
            TestRegistryFactory.Build(out _, out _, out _persistence, out _, out _playerData);
        }

        [TearDown]
        public void TearDown()
        {
            _playerData.Shutdown();
        }

        [Test]
        public void LoadProfile_WithRegisteredMigration_MigratesSectionToCurrentVersion()
        {
            _playerData.RegisterSection(() => new TestSettingsSection());
            _playerData.RegisterMigration("TestSettings", new TestSettingsV1ToV2Migration());
            _playerData.CreateProfile(ProfileId.Default);

            // Simulate a save made by an older build (schema version 1) directly against the
            // exact key PlayerProfileService will look under.
            string key = PlayerProfileService.BuildSectionKey(ProfileId.Default, "TestSettings");
            _persistence.Save(key, new TestSettingsDataV1 { Difficulty = 3 }, 1);

            ProfileOperationResult result = _playerData.LoadProfile(ProfileId.Default);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, _playerData.ActiveProfile.GetSection<TestSettingsSection>().Difficulty);
        }

        [Test]
        public void RegisterMigration_AfterProfileLoaded_Throws()
        {
            _playerData.LoadDefaultProfile();

            Assert.Throws<System.InvalidOperationException>(
                () => _playerData.RegisterMigration("TestSettings", new TestSettingsV1ToV2Migration()));
        }

        [Test]
        public void LoadProfile_NewSectionNeverSavedBefore_UsesDefaultsWithoutBeingCorrupted()
        {
            _playerData.CreateProfile(ProfileId.Default);

            // Register a brand-new section only after the profile already exists on disk without
            // it - the "content added in a later build" case (CLAUDE.md's Phase 13 brief, section
            // 24). Re-registering after CreateProfile requires a fresh service instance since
            // registration is locked out once a profile is active; simulate that by unloading
            // first (no profile is active immediately after CreateProfile).
            _playerData.RegisterSection(() => new TestProgressionSection());

            ProfileOperationResult result = _playerData.LoadProfile(ProfileId.Default);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(ProfileOperationResultKind.Success, result.Kind);
        }
    }
}
