using System;

namespace GameFramework.RemoteConfig.Providers
{
    /// <summary>
    /// The remote config provider boundary - see CLAUDE.md's Phase 17 brief, sections 21-23. No
    /// Firebase Remote Config/Unity Remote Config/PlayFab adapter exists in this project (none of
    /// those SDKs is installed - see <c>Packages/manifest.json</c>); a future adapter lives in its own
    /// assembly (e.g. <c>GameFramework.RemoteConfig.Firebase</c>), referencing only that installed SDK,
    /// implementing this interface against its real API. <see cref="RemoteConfigService"/> owns all
    /// game-facing configuration logic (precedence, validation, atomicity, caching); a provider is
    /// only ever asked "initialize" and "fetch".
    /// </summary>
    public interface IRemoteConfigProvider
    {
        /// <summary>Called once during <see cref="IRemoteConfigService"/> initialization. Must not
        /// block; report success/failure via <paramref name="onComplete"/>.</summary>
        void Initialize(Action<bool> onComplete);

        /// <summary>
        /// Fetches the latest configuration. <paramref name="currentSchemaVersion"/> is the highest
        /// schema this application supports (see <see cref="RemoteConfigConfiguration.SupportedSchemaVersion"/>)
        /// - a provider capable of schema negotiation may use it to request a compatible payload, but
        /// is not required to; <see cref="RemoteConfigService"/> re-validates the returned
        /// <see cref="RemoteConfigProviderResult.SchemaVersion"/> regardless. Must eventually call
        /// <paramref name="onComplete"/> exactly once; if it never does (e.g. a genuinely hung
        /// network call), <see cref="RemoteConfigService"/>'s own fetch-timeout still protects the
        /// game (see CLAUDE.md's Phase 17 brief, section 26) and silently ignores a late callback that
        /// arrives afterward.
        /// </summary>
        void Fetch(int currentSchemaVersion, Action<RemoteConfigProviderResult> onComplete);
    }
}
