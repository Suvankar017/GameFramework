using System.Collections.Generic;
using GameFramework.Runtime.Events;
using NUnit.Framework;

namespace GameFramework.PlayerData.Tests
{
    /// <summary>Event-ordering coverage, mirroring every other phase's own <c>IntegrationTests.cs</c>
    /// (compare <c>GameFlow.Tests.IntegrationTests</c>, <c>Tutorials.Tests.IntegrationTests</c>).</summary>
    public class IntegrationTests
    {
        private EventService _events;
        private PlayerProfileService _playerData;
        private List<string> _log;

        [SetUp]
        public void SetUp()
        {
            // NUnit reuses one fixture instance across every [Test] in this class by default, so
            // _log must be rebuilt here rather than at field-initializer time - otherwise entries
            // from an earlier test in the same run would still be present.
            _log = new List<string>();

            TestRegistryFactory.Build(out _, out _events, out _, out _, out _playerData);
            _playerData.RegisterSection(() => new TestProgressionSection());

            _events.Subscribe<ProfileLoadingEvent>(_ => _log.Add("Loading"));
            _events.Subscribe<ProfileLoadedEvent>(_ => _log.Add("Loaded"));
            _events.Subscribe<ProfileSavingEvent>(_ => _log.Add("Saving"));
            _events.Subscribe<ProfileSavedEvent>(_ => _log.Add("Saved"));
            _events.Subscribe<ProfileUnloadingEvent>(_ => _log.Add("Unloading"));
            _events.Subscribe<ProfileUnloadedEvent>(_ => _log.Add("Unloaded"));
            _events.Subscribe<ProfileSwitchedEvent>(_ => _log.Add("Switched"));
        }

        [TearDown]
        public void TearDown()
        {
            _playerData.Shutdown();
        }

        [Test]
        public void LoadThenSaveThenUnload_PublishesEventsInOrder()
        {
            _playerData.LoadDefaultProfile();
            _playerData.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(1);
            _playerData.Save();
            _playerData.UnloadActiveProfile(saveIfDirty: false);

            CollectionAssert.AreEqual(
                new[] { "Loading", "Loaded", "Saving", "Saved", "Unloading", "Unloaded" },
                _log);
        }

        [Test]
        public void SwitchProfile_PublishesUnloadThenLoadThenSwitched()
        {
            _playerData.CreateProfile(new ProfileId("A"));
            _playerData.CreateProfile(new ProfileId("B"));
            _playerData.LoadProfile(new ProfileId("A"));
            _log.Clear();

            _playerData.SwitchProfile(new ProfileId("B"));

            CollectionAssert.AreEqual(
                new[] { "Unloading", "Unloaded", "Loading", "Loaded", "Switched" },
                _log);
        }

        [Test]
        public void DirectCSharpEvents_MirrorTheEventServicePublishes()
        {
            var directLog = new List<string>();
            _playerData.ProfileLoading += _ => directLog.Add("Loading");
            _playerData.ProfileLoaded += _ => directLog.Add("Loaded");

            _playerData.LoadDefaultProfile();

            CollectionAssert.AreEqual(new[] { "Loading", "Loaded" }, directLog);
        }
    }
}
