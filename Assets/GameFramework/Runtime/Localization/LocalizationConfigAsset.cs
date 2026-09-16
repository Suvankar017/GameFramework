using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Localization
{
    /// <summary>Which language tables exist and which one is the default/fallback. A game
    /// registers exactly one of these with <see cref="ILocalizationService"/>.</summary>
    [CreateAssetMenu(menuName = "GameFramework/Localization/Localization Config", fileName = "LocalizationConfig")]
    public sealed class LocalizationConfigAsset : ScriptableObject
    {
        public string DefaultLanguageCode;
        public List<LocalizationTableAsset> Tables = new List<LocalizationTableAsset>();
    }
}
