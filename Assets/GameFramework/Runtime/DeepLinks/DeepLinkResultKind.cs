namespace GameFramework.DeepLinks
{
    public enum DeepLinkResultKind
    {
        Handled,

        /// <summary>The link was well-formed but no registered handler claimed it, or a matching
        /// handler reported <see cref="DeepLinkHandlerResult.Failed"/>.</summary>
        NoHandlerFound,

        /// <summary>Malformed URI, or an empty/null input - see CLAUDE.md's Phase 18 brief, section
        /// 36 (all incoming data is untrusted).</summary>
        Rejected,

        /// <summary>Held as the one pending link until <see cref="IDeepLinkService.SetReady"/> is
        /// called - see CLAUDE.md's Phase 18 brief, section 22.</summary>
        Deferred,

        /// <summary>The identical raw URI as the immediately-previous call - see CLAUDE.md's Phase 18
        /// brief, section 23, and <see cref="DeepLinkService"/>'s remarks on the exact dedupe rule.</summary>
        Duplicate
    }
}
