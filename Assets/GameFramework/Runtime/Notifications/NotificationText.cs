using GameFramework.Localization;

namespace GameFramework.Notifications
{
    /// <summary>
    /// One piece of notification text (title/body/subtitle) - either raw or a Phase 3 localization
    /// key, never both - see CLAUDE.md's Phase 18 brief, section 27. This framework never builds a
    /// second localization system; <see cref="Resolve"/> only ever calls into the existing
    /// <see cref="ILocalizationService"/>.
    ///
    /// <b>Resolved at schedule time, not display time</b> (section 27's own warning): a local
    /// notification can be delivered by the OS while the game process isn't even running, so there is
    /// no opportunity to call <see cref="ILocalizationService"/> at that moment. <see cref="NotificationService.Schedule"/>
    /// resolves every <see cref="NotificationContent"/> field to concrete text against the
    /// currently-active language before it ever reaches a provider - a later language change has no
    /// effect on an already-scheduled notification's text (the same limitation every real mobile OS
    /// imposes on local notifications).
    /// </summary>
    public readonly struct NotificationText
    {
        public readonly string RawText;
        public readonly string LocalizationKey;
        public readonly object[] LocalizationParameters;

        private NotificationText(string rawText, string localizationKey, object[] localizationParameters)
        {
            RawText = rawText;
            LocalizationKey = localizationKey;
            LocalizationParameters = localizationParameters;
        }

        public static NotificationText FromText(string rawText) => new NotificationText(rawText ?? string.Empty, null, null);

        public static NotificationText FromLocalizationKey(string key, params object[] parameters) =>
            new NotificationText(null, key, parameters);

        public bool IsEmpty => string.IsNullOrEmpty(RawText) && string.IsNullOrEmpty(LocalizationKey);

        /// <summary>Resolves to concrete display text. <paramref name="localization"/> may be null
        /// (Localization not registered) - a localization-key request then falls back to the raw key
        /// itself, logged once by the caller rather than throwing (section 55: never crash for a
        /// missing optional dependency).</summary>
        public string Resolve(ILocalizationService localization)
        {
            if (string.IsNullOrEmpty(LocalizationKey))
            {
                return RawText ?? string.Empty;
            }

            if (localization == null)
            {
                return LocalizationKey;
            }

            return LocalizationParameters != null && LocalizationParameters.Length > 0
                ? localization.Format(LocalizationKey, LocalizationParameters)
                : localization.GetString(LocalizationKey);
        }
    }
}
