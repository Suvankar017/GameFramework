using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GameFramework.Localization
{
    /// <summary>
    /// One language's translated strings/assets — pure data, no translations for any specific game
    /// ship with the framework itself. <see cref="FontAssetOverride"/> lets a language with
    /// different glyph requirements (e.g. CJK, Cyrillic) use its own TextMeshPro font asset
    /// instead of whatever a screen's default font is.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Localization/Localization Table", fileName = "LocalizationTable")]
    public sealed class LocalizationTableAsset : ScriptableObject
    {
        public string LanguageCode;
        public string DisplayName;
        public TMP_FontAsset FontAssetOverride;

        public List<LocalizationEntry> Entries = new List<LocalizationEntry>();
        public List<LocalizationAssetEntry> Assets = new List<LocalizationAssetEntry>();

        private Dictionary<string, string> _lookup;
        private Dictionary<string, UnityEngine.Object> _assetLookup;

        public bool TryGetString(string key, out string value)
        {
            BuildLookupIfNeeded();
            return _lookup.TryGetValue(key, out value);
        }

        public bool TryGetAsset(string key, out UnityEngine.Object asset)
        {
            BuildLookupIfNeeded();
            return _assetLookup.TryGetValue(key, out asset);
        }

        /// <summary>Rebuilds the lookup dictionaries from <see cref="Entries"/>/<see cref="Assets"/>.
        /// Called lazily on first lookup; call explicitly after mutating the lists at runtime
        /// (tables are normally authored, not mutated, so this is rarely needed outside tests).</summary>
        public void InvalidateLookup()
        {
            _lookup = null;
            _assetLookup = null;
        }

        private void BuildLookupIfNeeded()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<string, string>(Entries.Count);
            foreach (LocalizationEntry entry in Entries)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    _lookup[entry.Key] = entry.Value;
                }
            }

            _assetLookup = new Dictionary<string, UnityEngine.Object>(Assets.Count);
            foreach (LocalizationAssetEntry entry in Assets)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    _assetLookup[entry.Key] = entry.Asset;
                }
            }
        }

        private void OnEnable()
        {
            InvalidateLookup();
        }
    }
}
