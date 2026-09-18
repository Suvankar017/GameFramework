using UnityEngine;

namespace GameFramework.Presentation
{
    /// <summary>
    /// What gameplay code actually hands the presentation layer - deliberately small and strongly
    /// typed (see CLAUDE.md's Phase 10 brief, section 6) rather than a
    /// <c>Dictionary&lt;string, object&gt;</c> payload. Everything about *how* the request is
    /// presented lives in the paired <see cref="FeedbackDefinition"/>, authored separately - this
    /// struct only carries the *instance* data that varies call to call.
    /// </summary>
    public readonly struct FeedbackRequest
    {
        public readonly FeedbackId Id;

        /// <summary>Where the effect happened, for <see cref="AudioFeedbackConfig"/> (positional
        /// playback) and <see cref="VisualEffectFeedbackConfig"/> (spawn position). Null for a
        /// purely 2D-screen-space or non-positional request.</summary>
        public readonly Vector3? WorldPosition;

        /// <summary>Optional originating object, for a game's own diagnostics/event handlers -
        /// never inspected by this framework itself.</summary>
        public readonly Object Source;

        /// <summary>Normalized [0, 1] request-specific strength, further scaled by the player's
        /// global "Presentation.IntensityScale" accessibility setting - see
        /// <see cref="PresentationService"/>'s remarks on intensity.</summary>
        public readonly float Intensity;

        public FeedbackRequest(FeedbackId id, Vector3? worldPosition = null, Object source = null, float intensity = 1f)
        {
            Id = id;
            WorldPosition = worldPosition;
            Source = source;
            Intensity = Mathf.Clamp01(intensity);
        }
    }
}
