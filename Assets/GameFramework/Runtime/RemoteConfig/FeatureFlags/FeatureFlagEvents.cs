namespace GameFramework.RemoteConfig.FeatureFlags
{
    /// <summary>Published by <see cref="FeatureFlagService"/> through the Phase 2 Event System,
    /// mirroring its own <see cref="IFeatureFlagService.FlagChanged"/> C# event - see CLAUDE.md's
    /// Phase 17 brief, section 82.</summary>
    public readonly struct FeatureFlagChangedEvent
    {
        public readonly string Key;
        public readonly bool IsEnabled;

        public FeatureFlagChangedEvent(string key, bool isEnabled)
        {
            Key = key;
            IsEnabled = isEnabled;
        }
    }
}
