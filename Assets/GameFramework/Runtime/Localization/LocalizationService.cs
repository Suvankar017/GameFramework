using System;
using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using TMPro;
using UnityEngine;

namespace GameFramework.Localization
{
    /// <summary>
    /// Default <see cref="ILocalizationService"/>. The active language is stored as a normal
    /// <see cref="ISettingsService"/> string setting ("Localization.Language") rather than a
    /// bespoke persistence path, so it goes through the same save file, validation, and
    /// <see cref="Events.SettingChangedEvent"/> plumbing every other setting does.
    /// </summary>
    public sealed class LocalizationService : ILocalizationService
    {
        private const string LanguageSettingKey = "Localization.Language";
        private const string LogCategory = "Localization";

        private readonly Dictionary<string, LocalizationTableAsset> _tables = new Dictionary<string, LocalizationTableAsset>();
        private readonly HashSet<string> _warnedMissingKeys = new HashSet<string>();
        private readonly List<LanguageInfo> _availableLanguages = new List<LanguageInfo>();

        private ISettingsService _settings;
        private IEventService _events;
        private ILoggingService _log;
        private bool _languageSettingRegistered;

        public string CurrentLanguageCode { get; private set; }
        public string DefaultLanguageCode { get; private set; }
        public IReadOnlyList<LanguageInfo> AvailableLanguages => _availableLanguages;

        public void Initialize(IServiceRegistry registry)
        {
            _settings = registry.Get<ISettingsService>();
            _events = registry.Get<IEventService>();
            registry.TryGet(out _log);
        }

        public void Shutdown()
        {
            _tables.Clear();
            _availableLanguages.Clear();
            _warnedMissingKeys.Clear();
            _languageSettingRegistered = false;
            CurrentLanguageCode = null;
            DefaultLanguageCode = null;
        }

        public void LoadConfig(LocalizationConfigAsset config)
        {
            Guard.NotNull(config, nameof(config));
            Guard.NotNullOrEmpty(config.DefaultLanguageCode, nameof(config.DefaultLanguageCode));

            _tables.Clear();
            _availableLanguages.Clear();

            foreach (LocalizationTableAsset table in config.Tables)
            {
                if (table == null || string.IsNullOrEmpty(table.LanguageCode))
                {
                    _log?.Log(LogLevel.Warning, LogCategory, "Config contains a null table or a table with no language code; skipped.");
                    continue;
                }

                _tables[table.LanguageCode] = table;
                _availableLanguages.Add(new LanguageInfo(table.LanguageCode, table.DisplayName));
            }

            if (!_tables.ContainsKey(config.DefaultLanguageCode))
            {
                throw new InvalidOperationException(
                    $"LocalizationConfigAsset's default language '{config.DefaultLanguageCode}' has no matching table.");
            }

            DefaultLanguageCode = config.DefaultLanguageCode;

            // Registered here (not in Initialize) because the config, and therefore the set of
            // valid language codes the validator checks against, isn't known until a game supplies
            // one. Settings.Initialize's own automatic Load() already ran by this point, so this
            // setting must be explicitly re-Loaded to pick up a previously-persisted selection.
            if (!_languageSettingRegistered)
            {
                _settings.Register(new SettingDefinition<string>(
                    LanguageSettingKey, DefaultLanguageCode, v => _tables.ContainsKey(v), category: "Localization"));
                _languageSettingRegistered = true;
            }

            _settings.Load();

            CurrentLanguageCode = _tables.ContainsKey(_settings.Get<string>(LanguageSettingKey))
                ? _settings.Get<string>(LanguageSettingKey)
                : DefaultLanguageCode;
        }

        public void SetLanguage(string languageCode)
        {
            if (!_tables.ContainsKey(languageCode))
            {
                throw new ArgumentException($"Language '{languageCode}' is not available.", nameof(languageCode));
            }

            if (string.Equals(CurrentLanguageCode, languageCode, StringComparison.Ordinal))
            {
                return;
            }

            _settings.Set(LanguageSettingKey, languageCode);
            CurrentLanguageCode = languageCode;
            _events.Publish(new LanguageChangedEvent(languageCode));
        }

        public void ResetToDefault() => SetLanguage(DefaultLanguageCode);

        public string GetString(string key)
        {
            if (TryGetString(key, out string value))
            {
                return value;
            }

            if (_warnedMissingKeys.Add(key))
            {
                _log?.Log(LogLevel.Warning, LogCategory, $"Missing localization key '{key}'.");
            }

            // Debug builds (Editor and development builds) get a visibly-broken marker so a
            // missing key can never be mistaken for real content; release builds fall back to the
            // raw key instead, which at least stays readable in a screenshot from a player.
            return Debug.isDebugBuild ? $"[Missing Localization] {key}" : key;
        }

        public bool TryGetString(string key, out string value)
        {
            if (!string.IsNullOrEmpty(CurrentLanguageCode) &&
                _tables.TryGetValue(CurrentLanguageCode, out LocalizationTableAsset table) &&
                table.TryGetString(key, out value))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(DefaultLanguageCode) &&
                _tables.TryGetValue(DefaultLanguageCode, out LocalizationTableAsset defaultTable) &&
                defaultTable.TryGetString(key, out value))
            {
                return true;
            }

            value = null;
            return false;
        }

        public string Format(string key, params object[] args) => string.Format(GetString(key), args);

        public bool TryGetAsset<TAsset>(string key, out TAsset asset) where TAsset : UnityEngine.Object
        {
            if (TryGetRawAsset(CurrentLanguageCode, key, out UnityEngine.Object raw) ||
                TryGetRawAsset(DefaultLanguageCode, key, out raw))
            {
                asset = raw as TAsset;
                return asset != null;
            }

            asset = null;
            return false;
        }

        public TMP_FontAsset CurrentFontAsset =>
            !string.IsNullOrEmpty(CurrentLanguageCode) && _tables.TryGetValue(CurrentLanguageCode, out LocalizationTableAsset table)
                ? table.FontAssetOverride
                : null;

        private bool TryGetRawAsset(string languageCode, string key, out UnityEngine.Object asset)
        {
            if (!string.IsNullOrEmpty(languageCode) && _tables.TryGetValue(languageCode, out LocalizationTableAsset table))
            {
                return table.TryGetAsset(key, out asset);
            }

            asset = null;
            return false;
        }
    }
}
