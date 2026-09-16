namespace GameFramework.Localization
{
    /// <summary>
    /// Descriptive metadata for one available language. <see cref="Code"/> is the stable
    /// identifier used everywhere else (settings persistence, table lookup, save data) — never an
    /// array index or enum ordinal, so adding/removing/reordering languages can never corrupt a
    /// player's saved language selection.
    /// </summary>
    public readonly struct LanguageInfo
    {
        /// <summary>Stable language identifier, e.g. "en", "fr", "ja". Persisted in settings —
        /// treat as a compatibility contract like any other save data.</summary>
        public readonly string Code;

        /// <summary>Human-readable name for language-selection UI, e.g. "English".</summary>
        public readonly string DisplayName;

        public LanguageInfo(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }
    }
}
