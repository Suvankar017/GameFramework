using GameFramework.Runtime.Persistence;
using NUnit.Framework;

namespace GameFramework.PlayerData.Tests
{
    public class CorruptionAndBackupTests
    {
        private InMemoryPersistenceStorage _storage;

        [SetUp]
        public void SetUp()
        {
            TestRegistryFactory.Build(out _, out _, out _, out _, out _, out _storage);
        }

        [Test]
        public void LoadProfile_CorruptedSectionWithNoBackup_ResetsToDefaultsAndReportsCorrupted()
        {
            PlayerProfileService playerData = BuildSessionSharingStorage();
            playerData.RegisterSection(() => new TestProgressionSection());
            playerData.CreateProfile(ProfileId.Default);
            playerData.Shutdown();

            string key = PlayerProfileService.BuildSectionKey(ProfileId.Default, "TestProgression");
            _storage.WriteText(key, "not valid json {{{");

            PlayerProfileService secondSession = BuildSessionSharingStorage();
            secondSession.RegisterSection(() => new TestProgressionSection());

            ProfileOperationResult result = secondSession.LoadProfile(ProfileId.Default);

            Assert.AreEqual(ProfileOperationResultKind.Corrupted, result.Kind);
            Assert.AreEqual(0, secondSession.ActiveProfile.GetSection<TestProgressionSection>().Integer);
            secondSession.Shutdown();
        }

        [Test]
        public void LoadProfile_CorruptedSectionWithBackup_RestoresFromBackup()
        {
            PlayerProfileService playerData = BuildSessionSharingStorage();
            playerData.RegisterSection(() => new TestProgressionSection());
            playerData.LoadDefaultProfile();
            playerData.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(1);
            playerData.Save(); // backs up the on-disk default (0), then primary becomes 1
            playerData.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(2);
            playerData.Save(); // backs up the on-disk "1" before writing "2"
            playerData.Shutdown();

            string key = PlayerProfileService.BuildSectionKey(ProfileId.Default, "TestProgression");
            _storage.WriteText(key, "not valid json {{{");

            PlayerProfileService secondSession = BuildSessionSharingStorage();
            secondSession.RegisterSection(() => new TestProgressionSection());

            ProfileOperationResult result = secondSession.LoadProfile(ProfileId.Default);

            Assert.AreEqual(ProfileOperationResultKind.Corrupted, result.Kind);
            Assert.AreEqual(1, secondSession.ActiveProfile.GetSection<TestProgressionSection>().Integer);
            secondSession.Shutdown();
        }

        [Test]
        public void LoadProfile_NoCorruption_ReportsSuccessNotCorrupted()
        {
            PlayerProfileService playerData = BuildSessionSharingStorage();
            playerData.RegisterSection(() => new TestProgressionSection());
            playerData.LoadDefaultProfile();
            playerData.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(7);
            playerData.Save();
            playerData.Shutdown();

            PlayerProfileService secondSession = BuildSessionSharingStorage();
            secondSession.RegisterSection(() => new TestProgressionSection());

            ProfileOperationResult result = secondSession.LoadProfile(ProfileId.Default);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(7, secondSession.ActiveProfile.GetSection<TestProgressionSection>().Integer);
            secondSession.Shutdown();
        }

        [Test]
        public void Save_SectionValidateThrows_IsolatedToThatSectionAndReportedAsFailed()
        {
            PlayerProfileService playerData = BuildSessionSharingStorage();
            playerData.RegisterSection(() => new ThrowingSection());
            playerData.RegisterSection(() => new TestSettingsSection());
            playerData.LoadDefaultProfile();

            playerData.ActiveProfile.GetSection<TestSettingsSection>().SetDifficulty(5);
            var throwing = playerData.ActiveProfile.GetSection<ThrowingSection>();
            throwing.ForceDirty();
            throwing.ThrowOnValidate = true;

            ProfileOperationResult result = playerData.Save();

            Assert.AreEqual(ProfileOperationResultKind.Failed, result.Kind);
            // The healthy section's write must not be blocked by the other section's failure.
            Assert.IsFalse(playerData.ActiveProfile.GetSection<TestSettingsSection>().IsDirty);
            playerData.Shutdown();
        }

        private PlayerProfileService BuildSessionSharingStorage()
        {
            TestRegistryFactory.BuildWithStorage(_storage, out _, out _, out _, out _, out PlayerProfileService playerData);
            return playerData;
        }
    }
}
