namespace GameFramework.Presentation
{
    /// <summary>Outcome of <see cref="IPresentationService.Play"/>. Never thrown for a normal
    /// invalid/disallowed request.</summary>
    public enum PlayResult
    {
        /// <summary>The request was accepted and dispatched. Individual channels may still have
        /// silently skipped themselves (disabled channel, a lower-priority exclusive-channel
        /// conflict) - see <see cref="FeedbackPlayedEvent"/> if per-channel observability is ever
        /// needed; this framework does not report that level of detail on the return value itself,
        /// mirroring how <see cref="Audio.IAudioService.Play"/> does not report exactly which
        /// limiting rule suppressed a cue.</summary>
        Success,

        /// <summary>No <see cref="FeedbackDefinition"/> with that <see cref="FeedbackId"/> was
        /// registered via <see cref="IPresentationService.RegisterDefinition"/>.</summary>
        NotFound,

        /// <summary>The master "Presentation.Enabled" setting is off - no channel was attempted.</summary>
        Suppressed
    }
}
