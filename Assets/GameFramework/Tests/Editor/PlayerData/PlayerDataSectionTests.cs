using NUnit.Framework;

namespace GameFramework.PlayerData.Tests
{
    public class PlayerDataSectionTests
    {
        private PlayerProfileService _playerData;

        [SetUp]
        public void SetUp()
        {
            TestRegistryFactory.Build(out _, out _, out _, out _, out _playerData);
            _playerData.RegisterSection(() => new TestProgressionSection());
        }

        [TearDown]
        public void TearDown()
        {
            _playerData.Shutdown();
        }

        [Test]
        public void NewProfile_SectionStartsAtDefaults()
        {
            _playerData.LoadDefaultProfile();

            var section = _playerData.ActiveProfile.GetSection<TestProgressionSection>();

            Assert.AreEqual(0, section.Integer);
            Assert.AreEqual(0, section.Items.Count);
        }

        [Test]
        public void Mutation_MarksSectionAndProfileDirty()
        {
            _playerData.LoadDefaultProfile();
            var section = _playerData.ActiveProfile.GetSection<TestProgressionSection>();

            section.SetInteger(5);

            Assert.IsTrue(section.IsDirty);
            Assert.IsTrue(_playerData.IsDirty);
        }

        [Test]
        public void Save_ClearsDirtyFlag()
        {
            _playerData.LoadDefaultProfile();
            var section = _playerData.ActiveProfile.GetSection<TestProgressionSection>();
            section.SetInteger(5);

            _playerData.Save();

            Assert.IsFalse(section.IsDirty);
            Assert.IsFalse(_playerData.IsDirty);
        }

        [Test]
        public void SaveThenReload_RoundTripsSectionData()
        {
            _playerData.LoadDefaultProfile();
            _playerData.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(42);
            _playerData.Save();
            _playerData.UnloadActiveProfile(saveIfDirty: false);

            _playerData.LoadDefaultProfile();

            Assert.AreEqual(42, _playerData.ActiveProfile.GetSection<TestProgressionSection>().Integer);
        }

        [Test]
        public void GetSection_UnregisteredType_Throws()
        {
            _playerData.LoadDefaultProfile();

            Assert.Throws<System.InvalidOperationException>(
                () => _playerData.ActiveProfile.GetSection<TestSettingsSection>());
        }

        [Test]
        public void TryGetSection_UnregisteredType_ReturnsFalse()
        {
            _playerData.LoadDefaultProfile();

            bool found = _playerData.ActiveProfile.TryGetSection(out TestSettingsSection section);

            Assert.IsFalse(found);
            Assert.IsNull(section);
        }

        [Test]
        public void Validate_IsCalledOnResetAndLoad()
        {
            _playerData.LoadDefaultProfile();
            var section = _playerData.ActiveProfile.GetSection<TestProgressionSection>();

            Assert.GreaterOrEqual(section.ValidateCallCount, 1);
        }

        [Test]
        public void Save_WithNothingDirty_IsNoOpSuccess()
        {
            _playerData.LoadDefaultProfile();

            ProfileOperationResult result = _playerData.Save();

            Assert.IsTrue(result.Success);
        }
    }
}
