using NUnit.Framework;

namespace GameFramework.PlayerData.Tests
{
    public class AutosaveTests
    {
        private FakeTimeService _time;
        private Runtime.Timers.TimerService _timer;
        private PlayerProfileService _playerData;

        [SetUp]
        public void SetUp()
        {
            TestRegistryFactory.Build(out _time, out _, out _, out _timer, out _playerData);
            _playerData.RegisterSection(() => new TestProgressionSection());
        }

        [TearDown]
        public void TearDown()
        {
            _playerData.Shutdown();
        }

        private void AdvanceUnscaledTime(float seconds)
        {
            _time.UnscaledDeltaTime = seconds;
            _timer.Tick();
        }

        [Test]
        public void DirtyMutation_SchedulesDebouncedAutosave()
        {
            _playerData.AutosavePolicy = new AutosavePolicy { DebounceSeconds = 1f };
            _playerData.LoadDefaultProfile();

            _playerData.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(1);

            Assert.IsTrue(_playerData.GetDiagnostics().AutosavePending);
            Assert.IsTrue(_playerData.IsDirty);

            AdvanceUnscaledTime(1f);

            Assert.IsFalse(_playerData.IsDirty);
        }

        [Test]
        public void RapidMutations_CoalesceIntoOneDebouncedSave()
        {
            _playerData.AutosavePolicy = new AutosavePolicy { DebounceSeconds = 1f };
            _playerData.LoadDefaultProfile();
            var section = _playerData.ActiveProfile.GetSection<TestProgressionSection>();

            section.SetInteger(1);
            AdvanceUnscaledTime(0.6f); // not yet elapsed
            section.SetInteger(2); // reschedules - coalesced, not a second queued save
            AdvanceUnscaledTime(0.6f); // still not 1s since the reschedule
            Assert.IsTrue(_playerData.IsDirty);

            AdvanceUnscaledTime(0.5f); // now past 1s since the reschedule
            Assert.IsFalse(_playerData.IsDirty);
            Assert.AreEqual(2, section.Integer);
        }

        [Test]
        public void ManualOnlyPolicy_NeverAutosaves()
        {
            _playerData.AutosavePolicy = AutosavePolicy.ManualOnly;
            _playerData.LoadDefaultProfile();

            _playerData.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(1);
            AdvanceUnscaledTime(100f);

            Assert.IsTrue(_playerData.IsDirty);
        }

        [Test]
        public void ApplicationPauseTrigger_FlushesImmediatelyWhenEnabled()
        {
            _playerData.AutosavePolicy = new AutosavePolicy { Triggers = AutosaveTriggers.ApplicationPause };
            _playerData.LoadDefaultProfile();
            _playerData.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(1);

            _playerData.HandleApplicationPaused();

            Assert.IsFalse(_playerData.IsDirty);
        }

        [Test]
        public void FocusLostTrigger_Disabled_DoesNotFlush()
        {
            _playerData.AutosavePolicy = new AutosavePolicy { Triggers = AutosaveTriggers.ApplicationPause };
            _playerData.LoadDefaultProfile();
            _playerData.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(1);

            _playerData.HandleFocusLost();

            Assert.IsTrue(_playerData.IsDirty);
        }

        [Test]
        public void ApplicationQuit_AlwaysFlushesRegardlessOfPolicy()
        {
            _playerData.AutosavePolicy = AutosavePolicy.ManualOnly;
            _playerData.LoadDefaultProfile();
            _playerData.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(1);

            _playerData.HandleApplicationQuitting();

            Assert.IsFalse(_playerData.IsDirty);
        }

        [Test]
        public void UnloadActiveProfile_SavesDirtyDataFirstByDefault()
        {
            _playerData.LoadDefaultProfile();
            _playerData.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(9);

            _playerData.UnloadActiveProfile();
            _playerData.LoadDefaultProfile();

            Assert.AreEqual(9, _playerData.ActiveProfile.GetSection<TestProgressionSection>().Integer);
        }

        [Test]
        public void UnloadActiveProfile_SaveIfDirtyFalse_DiscardsUnsavedChanges()
        {
            _playerData.LoadDefaultProfile();
            _playerData.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(9);

            _playerData.UnloadActiveProfile(saveIfDirty: false);
            _playerData.LoadDefaultProfile();

            Assert.AreEqual(0, _playerData.ActiveProfile.GetSection<TestProgressionSection>().Integer);
        }
    }
}
