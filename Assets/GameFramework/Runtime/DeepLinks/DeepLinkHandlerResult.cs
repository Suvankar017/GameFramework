namespace GameFramework.DeepLinks
{
    /// <summary>See CLAUDE.md's Phase 18 brief, section 20.</summary>
    public enum DeepLinkHandlerResult
    {
        /// <summary>This handler does not recognize the link - <see cref="DeepLinkService"/> tries
        /// the next registered handler.</summary>
        NotApplicable,

        /// <summary>This handler recognized and fully processed the link - the dispatch chain stops
        /// here.</summary>
        Handled,

        /// <summary>This handler recognized the link but could not process it (e.g. an invalid
        /// parameter) - the dispatch chain stops here and is reported as rejected, rather than
        /// silently falling through to a lower-priority handler.</summary>
        Failed
    }
}
