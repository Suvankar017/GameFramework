using System;

namespace GameFramework.RemoteConfig.Providers
{
    /// <summary>
    /// The default provider when no remote config SDK is installed - see CLAUDE.md's Phase 17 brief,
    /// section 23. Initializes successfully (there is nothing to fail) but every
    /// <see cref="Fetch"/> reports failure, so <see cref="RemoteConfigService"/> always keeps whatever
    /// defaults/cache are already active. This is what keeps the framework fully usable (typed
    /// access, local defaults, feature flags, Live Ops against authored schedules) with zero external
    /// SDK present.
    /// </summary>
    public sealed class NoOpRemoteConfigProvider : IRemoteConfigProvider
    {
        public void Initialize(Action<bool> onComplete) => onComplete?.Invoke(true);

        public void Fetch(int currentSchemaVersion, Action<RemoteConfigProviderResult> onComplete) =>
            onComplete?.Invoke(RemoteConfigProviderResult.Failed("No remote config provider is installed (NoOpRemoteConfigProvider)."));
    }
}
