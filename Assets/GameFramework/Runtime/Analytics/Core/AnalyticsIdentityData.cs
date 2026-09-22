using System;

namespace GameFramework.Analytics
{
    /// <summary>Persisted anonymous analytics identity - see CLAUDE.md's Phase 16 brief, section 13.
    /// Deliberately not a device id/advertising id/hardware serial; an application-generated GUID,
    /// stored under its own <c>IPersistenceService</c> key, independent of any player-profile/account
    /// system (see <see cref="AnalyticsService"/>'s remarks on why this isn't retrofitted onto
    /// <c>PlayerData</c>).</summary>
    [Serializable]
    public sealed class AnalyticsIdentityData
    {
        public string UserId;
    }
}
