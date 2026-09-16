namespace GameFramework.Localization
{
    /// <summary>Published via <c>IEventService</c> whenever the active language actually changes
    /// (setting it to the language already active is a no-op — no event).</summary>
    public readonly struct LanguageChangedEvent
    {
        public readonly string LanguageCode;

        public LanguageChangedEvent(string languageCode)
        {
            LanguageCode = languageCode;
        }
    }
}
