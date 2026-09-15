using System;
using UnityEngine;

namespace GameFramework.Localization
{
    [Serializable]
    public struct LocalizationEntry
    {
        public string Key;
        [TextArea] public string Value;
    }

    /// <summary>A key mapped to a language-specific asset (sprite, audio clip, font, ...) rather
    /// than text — backs <c>LocalizedImage</c>/<c>LocalizedAsset</c>-style components.</summary>
    [Serializable]
    public struct LocalizationAssetEntry
    {
        public string Key;
        public UnityEngine.Object Asset;
    }
}
