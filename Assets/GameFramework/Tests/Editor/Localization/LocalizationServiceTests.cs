using System;
using GameFramework.Localization;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Localization.Tests
{
    public class LocalizationServiceTests
    {
        private InMemoryPersistenceStorage _storage;
        private PersistenceService _persistence;
        private EventService _events;
        private SettingsService _settings;
        private LocalizationService _localization;

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemoryPersistenceStorage();
            _persistence = new PersistenceService(_storage, new JsonPersistenceSerializer());
            _events = new EventService();
            _settings = new SettingsService();
            _localization = new LocalizationService();

            ServiceRegistry registry = BuildInitializedRegistry(_persistence, _events, _settings);
            _localization.Initialize(registry);
        }

        private static ServiceRegistry BuildInitializedRegistry(
            PersistenceService persistence, EventService events, SettingsService settings)
        {
            var registry = new ServiceRegistry();

            registry.Register<IPersistenceService>(persistence);
            registry.MarkInitialized(typeof(IPersistenceService));
            persistence.Initialize(registry);

            registry.Register<IEventService>(events);
            registry.MarkInitialized(typeof(IEventService));
            events.Initialize(registry);

            registry.Register<ISettingsService>(settings);
            registry.MarkInitialized(typeof(ISettingsService));
            settings.Initialize(registry);

            return registry;
        }

        private static LocalizationTableAsset BuildTable(string code, string displayName, params (string key, string value)[] entries)
        {
            var table = ScriptableObject.CreateInstance<LocalizationTableAsset>();
            table.LanguageCode = code;
            table.DisplayName = displayName;
            foreach ((string key, string value) in entries)
            {
                table.Entries.Add(new LocalizationEntry { Key = key, Value = value });
            }

            return table;
        }

        private static LocalizationConfigAsset BuildConfig(string defaultCode, params LocalizationTableAsset[] tables)
        {
            var config = ScriptableObject.CreateInstance<LocalizationConfigAsset>();
            config.DefaultLanguageCode = defaultCode;
            config.Tables.AddRange(tables);
            return config;
        }

        [Test]
        public void LoadConfig_SetsCurrentLanguageToDefault()
        {
            var english = BuildTable("en", "English", ("UI.Play", "Play"));
            _localization.LoadConfig(BuildConfig("en", english));

            Assert.AreEqual("en", _localization.CurrentLanguageCode);
            Assert.AreEqual("en", _localization.DefaultLanguageCode);
        }

        [Test]
        public void LoadConfig_MissingDefaultTable_Throws()
        {
            var english = BuildTable("en", "English");

            Assert.Throws<InvalidOperationException>(() => _localization.LoadConfig(BuildConfig("fr", english)));
        }

        [Test]
        public void GetString_KeyExistsInCurrentLanguage_ReturnsValue()
        {
            var english = BuildTable("en", "English", ("UI.Play", "Play"));
            _localization.LoadConfig(BuildConfig("en", english));

            Assert.AreEqual("Play", _localization.GetString("UI.Play"));
        }

        [Test]
        public void GetString_MissingInCurrentLanguage_FallsBackToDefaultLanguage()
        {
            var english = BuildTable("en", "English", ("UI.Play", "Play"));
            var french = BuildTable("fr", "French"); // no entries at all
            _localization.LoadConfig(BuildConfig("en", english, french));
            _localization.SetLanguage("fr");

            Assert.AreEqual("Play", _localization.GetString("UI.Play"));
        }

        [Test]
        public void GetString_MissingEverywhere_ReturnsVisibleMarkerInDebugBuilds()
        {
            var english = BuildTable("en", "English");
            _localization.LoadConfig(BuildConfig("en", english));

            string result = _localization.GetString("UI.Missing");

            if (Debug.isDebugBuild)
            {
                Assert.AreEqual("[Missing Localization] UI.Missing", result);
            }
            else
            {
                Assert.AreEqual("UI.Missing", result);
            }
        }

        [Test]
        public void TryGetString_MissingKey_ReturnsFalse()
        {
            var english = BuildTable("en", "English");
            _localization.LoadConfig(BuildConfig("en", english));

            Assert.IsFalse(_localization.TryGetString("Nope", out _));
        }

        [Test]
        public void SetLanguage_UnknownCode_Throws()
        {
            var english = BuildTable("en", "English");
            _localization.LoadConfig(BuildConfig("en", english));

            Assert.Throws<ArgumentException>(() => _localization.SetLanguage("de"));
        }

        [Test]
        public void SetLanguage_ValidCode_UpdatesCurrentLanguageAndPublishesEvent()
        {
            var english = BuildTable("en", "English");
            var french = BuildTable("fr", "French");
            _localization.LoadConfig(BuildConfig("en", english, french));

            string changedTo = null;
            _events.Subscribe<LanguageChangedEvent>(e => changedTo = e.LanguageCode);

            _localization.SetLanguage("fr");

            Assert.AreEqual("fr", _localization.CurrentLanguageCode);
            Assert.AreEqual("fr", changedTo);
        }

        [Test]
        public void SetLanguage_SameAsCurrent_DoesNotPublishEvent()
        {
            var english = BuildTable("en", "English");
            _localization.LoadConfig(BuildConfig("en", english));

            int changeCount = 0;
            _events.Subscribe<LanguageChangedEvent>(_ => changeCount++);

            _localization.SetLanguage("en");

            Assert.AreEqual(0, changeCount);
        }

        [Test]
        public void ResetToDefault_RestoresDefaultLanguage()
        {
            var english = BuildTable("en", "English");
            var french = BuildTable("fr", "French");
            _localization.LoadConfig(BuildConfig("en", english, french));
            _localization.SetLanguage("fr");

            _localization.ResetToDefault();

            Assert.AreEqual("en", _localization.CurrentLanguageCode);
        }

        [Test]
        public void SelectedLanguage_PersistsAcrossServiceInstances()
        {
            var english = BuildTable("en", "English");
            var french = BuildTable("fr", "French");
            _localization.LoadConfig(BuildConfig("en", english, french));
            _localization.SetLanguage("fr");
            _settings.Save();

            var reloadedSettings = new SettingsService();
            var reloadedLocalization = new LocalizationService();
            ServiceRegistry registry = BuildInitializedRegistry(_persistence, _events, reloadedSettings);
            reloadedLocalization.Initialize(registry);
            reloadedLocalization.LoadConfig(BuildConfig("en",
                BuildTable("en", "English"), BuildTable("fr", "French")));

            Assert.AreEqual("fr", reloadedLocalization.CurrentLanguageCode);
        }

        [Test]
        public void AvailableLanguages_ReflectsConfiguredTables()
        {
            var english = BuildTable("en", "English");
            var french = BuildTable("fr", "French");
            _localization.LoadConfig(BuildConfig("en", english, french));

            Assert.AreEqual(2, _localization.AvailableLanguages.Count);
        }

        [Test]
        public void TryGetAsset_ReturnsTypedAssetFromCurrentLanguage()
        {
            var english = BuildTable("en", "English");
            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            english.Assets.Add(new LocalizationAssetEntry { Key = "Icon.Flag", Asset = sprite });
            _localization.LoadConfig(BuildConfig("en", english));

            bool found = _localization.TryGetAsset("Icon.Flag", out Sprite result);

            Assert.IsTrue(found);
            Assert.AreEqual(sprite, result);
        }
    }
}
