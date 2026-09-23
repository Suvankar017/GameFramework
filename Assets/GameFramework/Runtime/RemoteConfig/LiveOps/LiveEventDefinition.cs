using System;
using System.Globalization;
using UnityEngine;

namespace GameFramework.RemoteConfig.LiveOps
{
    /// <summary>
    /// One live event's local authored schedule/content - a plain serializable entry inside
    /// <see cref="LiveOpsConfiguration"/> rather than its own ScriptableObject asset (see
    /// <c>Monetization.Ads.AdPlacementConfig</c>'s remarks for the same reasoning). This is the safe
    /// local fallback schedule (section 7); <see cref="LiveOpsService"/> may additionally apply a
    /// remote override for <see cref="Enabled"/>/start/end per event - see that type's remarks.
    ///
    /// Start/end times are authored as round-trip ("o" format) UTC timestamp strings rather than a
    /// native <see cref="DateTime"/> field, since Unity cannot serialize <see cref="DateTime"/>
    /// directly - see CLAUDE.md's Phase 17 brief, section 42 (UTC internally; presentation converts
    /// to local time when needed).
    /// </summary>
    [Serializable]
    public sealed class LiveEventDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private bool _enabled = true;

        [Tooltip("UTC timestamp, round-trip format (e.g. 2026-06-01T00:00:00.0000000Z).")]
        [SerializeField] private string _startTimeUtc;

        [Tooltip("UTC timestamp, round-trip format (e.g. 2026-06-30T23:59:59.0000000Z).")]
        [SerializeField] private string _endTimeUtc;

        [SerializeField] private string _titleLocalizationKey;
        [SerializeField] private string _descriptionLocalizationKey;
        [SerializeField] private string _iconId;

        [Tooltip("Optional prefix a game can use to build its own remote config sub-keys for this event's associated configuration (see CLAUDE.md's Phase 17 brief, section 46). Purely conventional - not enforced or read by this framework.")]
        [SerializeField] private string _configKeyPrefix;

        public LiveEventId Id => new LiveEventId(_id);
        public bool Enabled => _enabled;
        public string TitleLocalizationKey => _titleLocalizationKey;
        public string DescriptionLocalizationKey => _descriptionLocalizationKey;
        public string IconId => _iconId;
        public string ConfigKeyPrefix => _configKeyPrefix;

        public bool TryGetStartUtc(out DateTime value) => TryParse(_startTimeUtc, out value);
        public bool TryGetEndUtc(out DateTime value) => TryParse(_endTimeUtc, out value);

        internal static bool TryParse(string text, out DateTime value)
        {
            if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value))
            {
                return false;
            }

            value = value.ToUniversalTime();
            return true;
        }
    }
}
