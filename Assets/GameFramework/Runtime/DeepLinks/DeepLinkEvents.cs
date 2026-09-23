namespace GameFramework.DeepLinks
{
    /// <summary>Published by <see cref="DeepLinkService"/> through the Phase 2 Event System, mirroring
    /// its own C# events - see CLAUDE.md's Phase 18 brief, section 29.</summary>
    public readonly struct DeepLinkReceivedEvent
    {
        public readonly string RawUri;
        public DeepLinkReceivedEvent(string rawUri) => RawUri = rawUri;
    }

    public readonly struct DeepLinkHandledEvent
    {
        public readonly string RawUri;
        public DeepLinkHandledEvent(string rawUri) => RawUri = rawUri;
    }

    public readonly struct DeepLinkRejectedEvent
    {
        public readonly string RawUri;
        public readonly string Reason;
        public DeepLinkRejectedEvent(string rawUri, string reason)
        {
            RawUri = rawUri;
            Reason = reason;
        }
    }
}
