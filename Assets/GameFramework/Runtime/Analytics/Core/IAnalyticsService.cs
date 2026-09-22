using System;
using System.Collections.Generic;
using GameFramework.Runtime.Services;

namespace GameFramework.Analytics
{
    /// <summary>
    /// Game-facing analytics API - see CLAUDE.md's Phase 16 brief, section 62: a game should be able
    /// to write <c>analytics.Track("level_completed", parameters)</c> without knowing anything about
    /// the provider underneath. Every method here is safe to call regardless of
    /// <see cref="IsEnabled"/>/<see cref="Consent"/>/provider health - see
    /// <see cref="AnalyticsService"/>'s remarks on failure isolation (section 82: analytics must
    /// never crash the game).
    /// </summary>
    public interface IAnalyticsService : IGameService
    {
        /// <summary>Master on/off switch, independent of <see cref="Consent"/> - see
        /// <see cref="SetEnabled"/>.</summary>
        bool IsEnabled { get; }

        ConsentState Consent { get; }

        /// <summary>Application-generated anonymous identity - see CLAUDE.md's Phase 16 brief,
        /// section 13. Never a device id/advertising id/hardware serial.</summary>
        string UserId { get; }

        /// <summary>Identifier of the current analytics session - see <see cref="AnalyticsService"/>'s
        /// remarks on session semantics (section 16).</summary>
        string SessionId { get; }

        event Action<ConsentState> ConsentChanged;

        /// <summary>Master on/off switch. Disabling stops all new <see cref="Track"/> calls from doing
        /// anything (not even validated/queued) but does not clear an already-granted-and-queued
        /// buffer or reset identity/consent - see CLAUDE.md's Phase 16 brief, section 38.</summary>
        void SetEnabled(bool enabled);

        /// <summary>Records a consent decision. See CLAUDE.md's Phase 16 brief, sections 35-37 for the
        /// exact behavior of each transition (in particular: denying consent discards any buffered
        /// events rather than sending them).</summary>
        void SetConsent(ConsentState consent);

        /// <summary>Generates a new anonymous <see cref="UserId"/>, persists it, and informs the
        /// provider - see CLAUDE.md's Phase 16 brief, section 14 (privacy requests, profile deletion,
        /// testing). Never called automatically by this framework.</summary>
        void ResetIdentity();

        /// <summary>
        /// Submits a custom or well-known (see <see cref="EventNames"/>) analytics event. Never
        /// throws - an invalid name/parameter is logged and dropped/sanitized rather than raised as
        /// an exception (see CLAUDE.md's Phase 16 brief, sections 9-10). <paramref name="parameters"/>
        /// values must be string/int/long/float/double/bool; anything else is dropped with a logged
        /// warning.
        /// </summary>
        void Track(string eventName, IReadOnlyDictionary<string, object> parameters = null);

        /// <summary>Convenience wrapper over <see cref="Track"/> that publishes
        /// <see cref="EventNames.ScreenView"/> with a <c>screen_name</c> parameter - see CLAUDE.md's
        /// Phase 16 brief, section 17. <paramref name="screenName"/> is a logical screen id, never a
        /// Unity scene name.</summary>
        void TrackScreenView(string screenName, IReadOnlyDictionary<string, object> parameters = null);

        /// <summary>Value must be string/int/long/float/double/bool - see CLAUDE.md's Phase 16 brief,
        /// section 12.</summary>
        void SetUserProperty(string key, object value);

        /// <summary>Attempts to send any events currently queued (see <see cref="ConsentPolicy.BufferUntilDecided"/>)
        /// to the provider. Also called automatically once consent becomes <see cref="ConsentState.Granted"/>.</summary>
        void Flush();

        AnalyticsDiagnostics GetDiagnostics();
    }
}
