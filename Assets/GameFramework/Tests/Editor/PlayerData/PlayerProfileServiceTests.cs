using NUnit.Framework;

namespace GameFramework.PlayerData.Tests
{
    public class PlayerProfileServiceTests
    {
        private PlayerProfileService _playerData;

        [SetUp]
        public void SetUp()
        {
            TestRegistryFactory.Build(out _, out _, out _, out _, out _playerData);
        }

        [TearDown]
        public void TearDown()
        {
            _playerData.Shutdown();
        }

        [Test]
        public void CreateProfile_NewId_Succeeds()
        {
            ProfileOperationResult result = _playerData.CreateProfile(new ProfileId("A"));

            Assert.IsTrue(result.Success);
            Assert.IsTrue(_playerData.ProfileExists(new ProfileId("A")));
        }

        [Test]
        public void CreateProfile_DuplicateId_ReturnsAlreadyExists()
        {
            _playerData.CreateProfile(new ProfileId("A"));

            ProfileOperationResult result = _playerData.CreateProfile(new ProfileId("A"));

            Assert.AreEqual(ProfileOperationResultKind.AlreadyExists, result.Kind);
        }

        [Test]
        public void LoadProfile_NotCreated_ReturnsNotFound()
        {
            ProfileOperationResult result = _playerData.LoadProfile(new ProfileId("Missing"));

            Assert.AreEqual(ProfileOperationResultKind.NotFound, result.Kind);
            Assert.AreEqual(ProfileState.Unloaded, _playerData.State);
        }

        [Test]
        public void LoadProfile_Existing_BecomesActiveAndLoaded()
        {
            _playerData.CreateProfile(new ProfileId("A"));

            ProfileOperationResult result = _playerData.LoadProfile(new ProfileId("A"));

            Assert.IsTrue(result.Success);
            Assert.AreEqual(ProfileState.Loaded, _playerData.State);
            Assert.AreEqual(new ProfileId("A"), _playerData.ActiveProfile.Id);
        }

        [Test]
        public void LoadProfile_WhileAnotherIsActive_ReturnsInvalidState()
        {
            _playerData.CreateProfile(new ProfileId("A"));
            _playerData.CreateProfile(new ProfileId("B"));
            _playerData.LoadProfile(new ProfileId("A"));

            ProfileOperationResult result = _playerData.LoadProfile(new ProfileId("B"));

            Assert.AreEqual(ProfileOperationResultKind.InvalidState, result.Kind);
            Assert.AreEqual(new ProfileId("A"), _playerData.ActiveProfile.Id);
        }

        [Test]
        public void LoadDefaultProfile_NeverCreated_CreatesAndLoadsIt()
        {
            ProfileOperationResult result = _playerData.LoadDefaultProfile();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(ProfileId.Default, _playerData.ActiveProfile.Id);
        }

        [Test]
        public void UnloadActiveProfile_NoActiveProfile_IsNoOpSuccess()
        {
            ProfileOperationResult result = _playerData.UnloadActiveProfile();

            Assert.IsTrue(result.Success);
        }

        [Test]
        public void UnloadActiveProfile_ClearsActiveProfile()
        {
            _playerData.LoadDefaultProfile();

            ProfileOperationResult result = _playerData.UnloadActiveProfile();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(ProfileState.Unloaded, _playerData.State);
            Assert.IsNull(_playerData.ActiveProfile);
        }

        [Test]
        public void SwitchProfile_MovesFromOneProfileToAnother()
        {
            _playerData.CreateProfile(new ProfileId("A"));
            _playerData.CreateProfile(new ProfileId("B"));
            _playerData.LoadProfile(new ProfileId("A"));

            ProfileOperationResult result = _playerData.SwitchProfile(new ProfileId("B"));

            Assert.IsTrue(result.Success);
            Assert.AreEqual(new ProfileId("B"), _playerData.ActiveProfile.Id);
        }

        [Test]
        public void SwitchProfile_ToSameProfile_IsNoOpSuccess()
        {
            _playerData.LoadDefaultProfile();

            ProfileOperationResult result = _playerData.SwitchProfile(ProfileId.Default);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(ProfileId.Default, _playerData.ActiveProfile.Id);
        }

        [Test]
        public void SwitchProfile_FromNoActiveProfile_LoadsRequested()
        {
            _playerData.CreateProfile(new ProfileId("A"));

            ProfileOperationResult result = _playerData.SwitchProfile(new ProfileId("A"));

            Assert.IsTrue(result.Success);
            Assert.AreEqual(new ProfileId("A"), _playerData.ActiveProfile.Id);
        }

        [Test]
        public void DeleteProfile_Existing_RemovesFromList()
        {
            _playerData.CreateProfile(new ProfileId("A"));

            ProfileOperationResult result = _playerData.DeleteProfile(new ProfileId("A"));

            Assert.IsTrue(result.Success);
            Assert.IsFalse(_playerData.ProfileExists(new ProfileId("A")));
        }

        [Test]
        public void DeleteProfile_Missing_ReturnsNotFound()
        {
            ProfileOperationResult result = _playerData.DeleteProfile(new ProfileId("Missing"));

            Assert.AreEqual(ProfileOperationResultKind.NotFound, result.Kind);
        }

        [Test]
        public void DeleteProfile_WhileActive_ReturnsInvalidState()
        {
            _playerData.LoadDefaultProfile();

            ProfileOperationResult result = _playerData.DeleteProfile(ProfileId.Default);

            Assert.AreEqual(ProfileOperationResultKind.InvalidState, result.Kind);
        }

        [Test]
        public void ListProfiles_ReturnsEveryCreatedProfile()
        {
            _playerData.CreateProfile(new ProfileId("A"));
            _playerData.CreateProfile(new ProfileId("B"));

            var profiles = _playerData.ListProfiles();

            Assert.AreEqual(2, profiles.Count);
        }

        [Test]
        public void Save_NoActiveProfile_ReturnsInvalidState()
        {
            ProfileOperationResult result = _playerData.Save();

            Assert.AreEqual(ProfileOperationResultKind.InvalidState, result.Kind);
        }

        [Test]
        public void CreateProfile_InvalidCharacterInId_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => _playerData.CreateProfile(new ProfileId("bad/id")));
        }

        [Test]
        public void RegisterSection_AfterProfileLoaded_Throws()
        {
            _playerData.LoadDefaultProfile();

            Assert.Throws<System.InvalidOperationException>(
                () => _playerData.RegisterSection(() => new TestProgressionSection()));
        }

        [Test]
        public void RegisterSection_DuplicateType_Throws()
        {
            _playerData.RegisterSection(() => new TestProgressionSection());

            Assert.Throws<System.InvalidOperationException>(
                () => _playerData.RegisterSection(() => new TestProgressionSection()));
        }
    }
}
