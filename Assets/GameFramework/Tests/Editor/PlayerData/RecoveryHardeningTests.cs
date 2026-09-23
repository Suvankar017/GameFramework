using System;
using GameFramework.Runtime.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.PlayerData.Tests
{
    /// <summary>Phase 19 data-loss prevention in <see cref="PlayerProfileService"/>: index recovery,
    /// orphan re-adoption, genuine backup detection, backup migration, post-load validation fallback,
    /// primary repair, and structured recovery reporting.</summary>
    public class RecoveryHardeningTests
    {
        /// <summary>Rejects (throws on) a negative value - stands in for a game's own post-load rule.</summary>
        private sealed class GuardedSection : PlayerDataSection<TestProgressionData>
        {
            public override string Id => "Guarded";
            public override int Version => 1;
            public int Integer => Data.Integer;

            public void SetInteger(int value)
            {
                Data.Integer = value;
                MarkDirty();
            }

            public override void Validate()
            {
                if (Data.Integer < 0)
                {
                    throw new InvalidOperationException("Negative value is not a valid state.");
                }
            }
        }

        private sealed class ReservedIdSection : PlayerDataSection<TestProgressionData>
        {
            public override string Id => "Meta";
            public override int Version => 1;
        }

        private sealed class SettingsV1ToV2 : ISaveMigration
        {
            public int FromVersion => 1;
            public int ToVersion => 2;

            public string Migrate(string payload)
            {
                var old = JsonUtility.FromJson<TestSettingsDataV1>(payload);
                return JsonUtility.ToJson(new TestSettingsData { Difficulty = old.Difficulty, TutorialsEnabled = true });
            }
        }

        private const string IndexKey = "GameFramework.PlayerData.Index";

        private InMemoryPersistenceStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemoryPersistenceStorage();
        }

        private PlayerProfileService NewSession()
        {
            TestRegistryFactory.BuildWithStorage(_storage, out _, out _, out _, out _, out PlayerProfileService playerData);
            return playerData;
        }

        private PersistenceService NewPersistence()
        {
            var persistence = new PersistenceService(_storage, new JsonPersistenceSerializer());
            persistence.Initialize(new GameFramework.Runtime.Services.ServiceRegistry());
            return persistence;
        }

        private void SeedDefaultProfileWithProgress(int value)
        {
            PlayerProfileService session = NewSession();
            session.RegisterSection(() => new TestProgressionSection());
            session.LoadDefaultProfile();
            session.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(value);
            session.Save();
            session.Shutdown();
        }

        [Test]
        public void CorruptedIndexWithNoBackup_LoadDefaultProfile_ReadoptsInsteadOfWipingProgress()
        {
            SeedDefaultProfileWithProgress(42);
            _storage.WriteText(IndexKey, "corrupt {{{");
            _storage.Delete(IndexKey + ".bak");

            PlayerProfileService session = NewSession();
            session.RegisterSection(() => new TestProgressionSection());
            ProfileOperationResult result = session.LoadDefaultProfile();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(42, session.ActiveProfile.GetSection<TestProgressionSection>().Integer,
                "A lost index must never cause the existing default profile to be overwritten.");
            session.Shutdown();
        }

        [Test]
        public void CorruptedIndex_RestoredFromIndexBackup()
        {
            PlayerProfileService first = NewSession();
            first.CreateProfile(new ProfileId("slot1"));
            first.CreateProfile(new ProfileId("slot2"));
            first.Shutdown();

            _storage.WriteText(IndexKey, "corrupt {{{");

            PlayerProfileService session = NewSession();
            Assert.AreEqual(2, session.ListProfiles().Count);
            session.Shutdown();
        }

        [Test]
        public void CorruptedBackup_IsNotReportedAsRestored_SectionResetInstead()
        {
            SeedDefaultProfileWithProgress(1);
            string key = PlayerProfileService.BuildSectionKey(ProfileId.Default, "TestProgression");
            _storage.WriteText(key, "corrupt primary");
            _storage.WriteText(key + ".bak", "corrupt backup");

            PlayerProfileService session = NewSession();
            session.RegisterSection(() => new TestProgressionSection());
            ProfileOperationResult result = session.LoadProfile(ProfileId.Default);

            Assert.AreEqual(ProfileOperationResultKind.Corrupted, result.Kind);
            Assert.AreEqual(1, session.LastLoadRecoveries.Count);
            Assert.AreEqual(SectionRecoveryKind.ResetToDefaults, session.LastLoadRecoveries[0].Kind);
            StringAssert.Contains("reset to defaults", result.Reason);
            session.Shutdown();
        }

        [Test]
        public void BackupWrittenByOlderBuild_IsMigratedWhenRestored()
        {
            PlayerProfileService first = NewSession();
            first.RegisterSection(() => new TestSettingsSection());
            first.RegisterMigration("TestSettings", new SettingsV1ToV2());
            first.CreateProfile(ProfileId.Default);
            first.Shutdown();

            string key = PlayerProfileService.BuildSectionKey(ProfileId.Default, "TestSettings");
            NewPersistence().Save(key + ".bak", new TestSettingsDataV1 { Difficulty = 4 }, 1);
            _storage.WriteText(key, "corrupt primary");

            PlayerProfileService session = NewSession();
            session.RegisterSection(() => new TestSettingsSection());
            session.RegisterMigration("TestSettings", new SettingsV1ToV2());
            session.LoadProfile(ProfileId.Default);

            Assert.AreEqual(4, session.ActiveProfile.GetSection<TestSettingsSection>().Difficulty);
            Assert.AreEqual(SectionRecoveryKind.RestoredFromBackup, session.LastLoadRecoveries[0].Kind);
            session.Shutdown();
        }

        [Test]
        public void PrimaryFailingPostLoadValidation_FallsBackToBackup()
        {
            PlayerProfileService first = NewSession();
            first.RegisterSection(() => new GuardedSection());
            first.LoadDefaultProfile();
            first.ActiveProfile.GetSection<GuardedSection>().SetInteger(5);
            first.Save();
            first.ActiveProfile.GetSection<GuardedSection>().SetInteger(6);
            first.Save(); // .bak now holds 5
            first.Shutdown();

            // Structurally valid, semantically invalid (e.g. an edited save file).
            string key = PlayerProfileService.BuildSectionKey(ProfileId.Default, "Guarded");
            NewPersistence().Save(key, new TestProgressionData { Integer = -999999 }, 1);

            PlayerProfileService session = NewSession();
            session.RegisterSection(() => new GuardedSection());
            ProfileOperationResult result = session.LoadProfile(ProfileId.Default);

            Assert.AreEqual(ProfileOperationResultKind.Corrupted, result.Kind);
            Assert.AreEqual(5, session.ActiveProfile.GetSection<GuardedSection>().Integer);
            session.Shutdown();
        }

        [Test]
        public void RestoreFromBackup_RepairsPrimarySoNextLoadIsClean()
        {
            PlayerProfileService first = NewSession();
            first.RegisterSection(() => new TestProgressionSection());
            first.LoadDefaultProfile();
            first.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(1);
            first.Save();
            first.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(2);
            first.Save();
            first.Shutdown();

            string key = PlayerProfileService.BuildSectionKey(ProfileId.Default, "TestProgression");
            _storage.WriteText(key, "corrupt primary");

            PlayerProfileService second = NewSession();
            second.RegisterSection(() => new TestProgressionSection());
            second.LoadProfile(ProfileId.Default);
            second.Shutdown();

            PlayerProfileService third = NewSession();
            third.RegisterSection(() => new TestProgressionSection());
            ProfileOperationResult result = third.LoadProfile(ProfileId.Default);

            Assert.IsTrue(result.Success, "The primary must have been repaired from the backup.");
            Assert.AreEqual(1, third.ActiveProfile.GetSection<TestProgressionSection>().Integer);
            Assert.AreEqual(0, third.LastLoadRecoveries.Count);
            third.Shutdown();
        }

        [Test]
        public void CorruptedPrimary_DoesNotOverwriteGoodBackupOnNextSave()
        {
            PlayerProfileService first = NewSession();
            first.RegisterSection(() => new TestProgressionSection());
            first.LoadDefaultProfile();
            first.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(1);
            first.Save();
            first.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(2);
            string key = PlayerProfileService.BuildSectionKey(ProfileId.Default, "TestProgression");
            string goodBackup = _storage.ReadText(key + ".bak");

            _storage.WriteText(key, "corrupted while running");
            first.Save();

            Assert.AreEqual(goodBackup, _storage.ReadText(key + ".bak"), "A corrupted primary must never become the backup.");
            first.Shutdown();
        }

        [Test]
        public void CorruptedMetadata_RestoredFromMetadataBackup()
        {
            PlayerProfileService first = NewSession();
            first.RegisterSection(() => new TestProgressionSection());
            first.LoadDefaultProfile();
            first.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(1);
            first.Save();
            first.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(2);
            first.Save(); // second save backs up the metadata written by the first
            first.Shutdown();

            _storage.WriteText(PlayerProfileService.BuildMetadataKey(ProfileId.Default), "corrupt");

            PlayerProfileService session = NewSession();
            session.RegisterSection(() => new TestProgressionSection());
            session.LoadProfile(ProfileId.Default);

            Assert.AreEqual(SectionRecovery.MetadataSectionId, session.LastLoadRecoveries[0].SectionId);
            Assert.AreEqual(SectionRecoveryKind.RestoredFromBackup, session.LastLoadRecoveries[0].Kind);
            session.Shutdown();
        }

        [Test]
        public void CleanLoad_ReportsNoRecoveries()
        {
            SeedDefaultProfileWithProgress(3);

            PlayerProfileService session = NewSession();
            session.RegisterSection(() => new TestProgressionSection());
            session.LoadProfile(ProfileId.Default);

            Assert.AreEqual(0, session.LastLoadRecoveries.Count);
            session.Shutdown();
        }

        [Test]
        public void IndexWithInvalidAndDuplicateEntries_IsSanitized()
        {
            var index = new PlayerProfileIndexData();
            index.ProfileIds.Add("good");
            index.ProfileIds.Add("good");
            index.ProfileIds.Add("");
            index.ProfileIds.Add("bad/id");
            NewPersistence().Save(IndexKey, index, 1);

            PlayerProfileService session = NewSession();

            Assert.AreEqual(1, session.ListProfiles().Count);
            Assert.AreEqual("good", session.ListProfiles()[0].Value);
            session.Shutdown();
        }

        [Test]
        public void RegisterSection_ReservedMetaId_Throws()
        {
            PlayerProfileService session = NewSession();

            Assert.Throws<ArgumentException>(() => session.RegisterSection(() => new ReservedIdSection()));
            session.Shutdown();
        }
    }
}
