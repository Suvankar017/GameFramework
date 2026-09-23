using System;
using GameFramework.Runtime.Services;

namespace GameFramework.RemoteConfig.FeatureFlags
{
    /// <summary>
    /// A thin, purpose-named layer over <see cref="IRemoteConfigService"/> - see CLAUDE.md's Phase 17
    /// brief, sections 32-35. <see cref="IsEnabled"/> is equivalent to
    /// <c>remoteConfig.GetBool(key, defaultValue)</c>; this interface exists so game code reads as
    /// "is this feature on" rather than "get this bool", and so <see cref="FlagChanged"/> gives a
    /// feature-flag-specific change notification without every caller needing to diff snapshots
    /// itself.
    ///
    /// Feature flags are never a security mechanism (section 34) - never gate secrets, privileged
    /// operations, purchase validation, or anti-cheat behind one; a player can always inspect a
    /// client-side value.
    /// </summary>
    public interface IFeatureFlagService : IGameService
    {
        /// <summary>Absence of remote configuration is never treated as "enabled" (section 33) -
        /// with no provider/no override, this returns whatever local default the flag declares (or
        /// <paramref name="defaultValue"/> for an undeclared, ad hoc flag key).</summary>
        bool IsEnabled(string key, bool defaultValue = false);

        /// <summary>
        /// Raised when a <i>declared</i> boolean <see cref="RemoteConfigDefinition"/>'s resolved
        /// value actually changes across a configuration activation - see CLAUDE.md's Phase 17 brief,
        /// section 35. Only covers keys with a registered definition (section 9/10: an undeclared, ad
        /// hoc flag key still works via <see cref="IsEnabled"/>, it just has no baseline to diff
        /// against and so never raises this event). This is a notification only - subscribing systems
        /// decide for themselves whether/when it is safe to react (section 36).
        /// </summary>
        event Action<string, bool> FlagChanged;
    }
}
