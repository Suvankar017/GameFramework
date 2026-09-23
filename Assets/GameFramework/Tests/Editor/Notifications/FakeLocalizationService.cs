using System.Collections.Generic;
using GameFramework.Localization;
using GameFramework.Runtime.Services;
using TMPro;
using UnityEngine;

namespace GameFramework.Notifications.Tests
{
    /// <summary>Minimal, fully controllable <see cref="ILocalizationService"/> test double - avoids
    /// needing a real <see cref="LocalizationConfigAsset"/>/table just to test
    /// <see cref="NotificationText"/> resolution.</summary>
    internal sealed class FakeLocalizationService : ILocalizationService
    {
        private readonly Dictionary<string, string> _table = new Dictionary<string, string>();

        public string CurrentLanguageCode => "en";
        public string DefaultLanguageCode => "en";
        public IReadOnlyList<LanguageInfo> AvailableLanguages => new List<LanguageInfo>(0);
        public TMP_FontAsset CurrentFontAsset => null;

        public void Set(string key, string value) => _table[key] = value;

        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
        }

        public void LoadConfig(LocalizationConfigAsset config)
        {
        }

        public void SetLanguage(string languageCode)
        {
        }

        public void ResetToDefault()
        {
        }

        public string GetString(string key) => _table.TryGetValue(key, out string value) ? value : $"!{key}!";

        public bool TryGetString(string key, out string value) => _table.TryGetValue(key, out value);

        public string Format(string key, params object[] args) => string.Format(GetString(key), args);

        public bool TryGetAsset<TAsset>(string key, out TAsset asset) where TAsset : Object
        {
            asset = null;
            return false;
        }
    }
}
