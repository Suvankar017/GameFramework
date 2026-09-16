using System.Collections.Generic;
using GameFramework.Runtime.Services;
using TMPro;

namespace GameFramework.Localization
{
    /// <summary>
    /// Key → localized value lookup: Localization Key → Localization Provider (the registered
    /// <see cref="LocalizationConfigAsset"/>'s tables) → Localized Value. Contains no translations
    /// of its own — a game supplies a <see cref="LocalizationConfigAsset"/> via
    /// <see cref="LoadConfig"/>. The current language is persisted through
    /// <c>Settings.ISettingsService</c>, not a bespoke mechanism.
    /// </summary>
    public interface ILocalizationService : IGameService
    {
        void LoadConfig(LocalizationConfigAsset config);

        string CurrentLanguageCode { get; }
        string DefaultLanguageCode { get; }
        IReadOnlyList<LanguageInfo> AvailableLanguages { get; }

        /// <summary>Throws <see cref="System.ArgumentException"/> if <paramref name="languageCode"/>
        /// isn't in <see cref="AvailableLanguages"/>. Setting the already-active language is a
        /// no-op (no <see cref="LanguageChangedEvent"/>).</summary>
        void SetLanguage(string languageCode);

        void ResetToDefault();

        /// <summary>
        /// Looks up <paramref name="key"/> in the current language's table, falling back to the
        /// default language's table, and finally to a visible missing-value marker. Never throws
        /// and never returns null. Missing keys are logged once each (category "Localization") to
        /// stay useful without spamming the console.
        /// </summary>
        string GetString(string key);

        bool TryGetString(string key, out string value);

        /// <summary><see cref="GetString"/> passed through <see cref="string.Format(string, object[])"/>.
        /// Falls back the same way as <see cref="GetString"/> before formatting.</summary>
        string Format(string key, params object[] args);

        bool TryGetAsset<TAsset>(string key, out TAsset asset) where TAsset : UnityEngine.Object;

        /// <summary>The current language's font override, or null if it doesn't specify one.</summary>
        TMP_FontAsset CurrentFontAsset { get; }
    }
}
