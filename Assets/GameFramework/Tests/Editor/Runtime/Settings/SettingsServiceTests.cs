using System;
using System.Collections.Generic;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using NUnit.Framework;

namespace GameFramework.Runtime.Tests.Settings
{
    public class SettingsServiceTests
    {
        private InMemoryPersistenceStorage _storage;
        private PersistenceService _persistence;
        private EventService _events;
        private SettingsService _settings;

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemoryPersistenceStorage();
            _persistence = new PersistenceService(_storage, new JsonPersistenceSerializer());
            _events = new EventService();
            _settings = new SettingsService();

            ServiceRegistry registry = BuildInitializedRegistry(_persistence, _events);
            _settings.Initialize(registry);
        }

        private static ServiceRegistry BuildInitializedRegistry(PersistenceService persistence, EventService events)
        {
            var registry = new ServiceRegistry();

            registry.Register<IPersistenceService>(persistence);
            registry.MarkInitialized(typeof(IPersistenceService));
            persistence.Initialize(registry);

            registry.Register<IEventService>(events);
            registry.MarkInitialized(typeof(IEventService));
            events.Initialize(registry);

            return registry;
        }

        [Test]
        public void Get_UnregisteredSetting_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _settings.Get<bool>("Vibration"));
        }

        [Test]
        public void Register_ThenGet_ReturnsDefaultValue()
        {
            _settings.Register(new SettingDefinition<bool>("Vibration", true));

            Assert.IsTrue(_settings.Get<bool>("Vibration"));
        }

        [Test]
        public void Register_DuplicateKey_Throws()
        {
            _settings.Register(new SettingDefinition<bool>("Vibration", true));

            Assert.Throws<InvalidOperationException>(
                () => _settings.Register(new SettingDefinition<bool>("Vibration", false)));
        }

        [Test]
        public void Register_DefaultFailsValidator_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => _settings.Register(new SettingDefinition<int>("Level", -1, v => v >= 0)));
        }

        [Test]
        public void Get_WrongType_Throws()
        {
            _settings.Register(new SettingDefinition<int>("Volume", 50));

            Assert.Throws<InvalidOperationException>(() => _settings.Get<bool>("Volume"));
        }

        [Test]
        public void TryGet_UnregisteredSetting_ReturnsFalse()
        {
            bool found = _settings.TryGet("Missing", out int value);

            Assert.IsFalse(found);
            Assert.AreEqual(0, value);
        }

        [Test]
        public void Set_ValidValue_UpdatesGet()
        {
            _settings.Register(new SettingDefinition<int>("Volume", 50));

            _settings.Set("Volume", 80);

            Assert.AreEqual(80, _settings.Get<int>("Volume"));
        }

        [Test]
        public void Set_InvalidValue_ThrowsAndDoesNotChangeValue()
        {
            _settings.Register(new SettingDefinition<int>("Volume", 50, v => v is >= 0 and <= 100));

            Assert.Throws<ArgumentException>(() => _settings.Set("Volume", 200));
            Assert.AreEqual(50, _settings.Get<int>("Volume"));
        }

        [Test]
        public void Set_SameValueAsCurrent_DoesNotPublishChangeEvent()
        {
            _settings.Register(new SettingDefinition<int>("Volume", 50));
            int changeCount = 0;
            _events.Subscribe<SettingChangedEvent>(_ => changeCount++);

            _settings.Set("Volume", 50);

            Assert.AreEqual(0, changeCount);
        }

        [Test]
        public void Set_DifferentValue_PublishesSettingChangedEventWithKey()
        {
            _settings.Register(new SettingDefinition<int>("Volume", 50));
            string changedKey = null;
            _events.Subscribe<SettingChangedEvent>(e => changedKey = e.Key);

            _settings.Set("Volume", 80);

            Assert.AreEqual("Volume", changedKey);
        }

        [Test]
        public void ResetToDefault_RestoresDefaultValue()
        {
            _settings.Register(new SettingDefinition<int>("Volume", 50));
            _settings.Set("Volume", 80);

            _settings.ResetToDefault("Volume");

            Assert.AreEqual(50, _settings.Get<int>("Volume"));
        }

        [Test]
        public void ResetAllToDefaults_RestoresEveryRegisteredSetting()
        {
            _settings.Register(new SettingDefinition<int>("Volume", 50));
            _settings.Register(new SettingDefinition<bool>("Vibration", true));
            _settings.Set("Volume", 10);
            _settings.Set("Vibration", false);

            _settings.ResetAllToDefaults();

            Assert.AreEqual(50, _settings.Get<int>("Volume"));
            Assert.IsTrue(_settings.Get<bool>("Vibration"));
        }

        [Test]
        public void GetKeysInCategory_ReturnsOnlyMatchingKeys()
        {
            _settings.Register(new SettingDefinition<int>("MasterVolume", 50, category: "Audio"));
            _settings.Register(new SettingDefinition<int>("MusicVolume", 50, category: "Audio"));
            _settings.Register(new SettingDefinition<bool>("Vibration", true, category: "Gameplay"));

            var audioKeys = new List<string>(_settings.GetKeysInCategory("Audio"));

            CollectionAssert.AreEquivalent(new[] { "MasterVolume", "MusicVolume" }, audioKeys);
        }

        [Test]
        public void Load_NoSaveExists_KeepsDefaults()
        {
            _settings.Register(new SettingDefinition<int>("Volume", 50));

            _settings.Load();

            Assert.AreEqual(50, _settings.Get<int>("Volume"));
        }

        [Test]
        public void SaveThenReloadInNewInstance_PersistsChangedValue()
        {
            _settings.Register(new SettingDefinition<int>("Volume", 50));
            _settings.Set("Volume", 77);
            _settings.Save();

            var reloaded = new SettingsService();
            ServiceRegistry registry = BuildInitializedRegistry(_persistence, _events);
            reloaded.Register(new SettingDefinition<int>("Volume", 50));
            reloaded.Initialize(registry); // Initialize loads automatically

            Assert.AreEqual(77, reloaded.Get<int>("Volume"));
        }

        [Test]
        public void Load_UnknownKeyInSavedData_IsIgnoredWithoutThrowing()
        {
            var legacyPersistence = new PersistenceService(_storage, new JsonPersistenceSerializer());
            var legacyEvents = new EventService();
            var legacy = new SettingsService();
            ServiceRegistry legacyRegistry = BuildInitializedRegistry(legacyPersistence, legacyEvents);
            legacy.Register(new SettingDefinition<bool>("RemovedFeatureToggle", true));
            legacy.Initialize(legacyRegistry);
            legacy.Set("RemovedFeatureToggle", false);
            legacy.Save();

            _settings.Register(new SettingDefinition<int>("Volume", 50));

            Assert.DoesNotThrow(() => _settings.Load());
            Assert.AreEqual(50, _settings.Get<int>("Volume"));
        }

        [Test]
        public void Shutdown_WhenDirty_AutoSavesChangedValue()
        {
            _settings.Register(new SettingDefinition<int>("Volume", 50));
            _settings.Set("Volume", 99);

            _settings.Shutdown();

            var reloaded = new SettingsService();
            ServiceRegistry registry = BuildInitializedRegistry(_persistence, _events);
            reloaded.Register(new SettingDefinition<int>("Volume", 50));
            reloaded.Initialize(registry);

            Assert.AreEqual(99, reloaded.Get<int>("Volume"));
        }

        [Test]
        public void Shutdown_WhenNotDirty_DoesNotThrow()
        {
            _settings.Register(new SettingDefinition<int>("Volume", 50));

            Assert.DoesNotThrow(() => _settings.Shutdown());
        }

        [Test]
        public void Shutdown_ClearsRegisteredSettings()
        {
            _settings.Register(new SettingDefinition<int>("Volume", 50));

            _settings.Shutdown();

            Assert.Throws<InvalidOperationException>(() => _settings.Get<int>("Volume"));
        }
    }
}
