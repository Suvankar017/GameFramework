namespace GameFramework.DeepLinks
{
    /// <summary>Development-time snapshot of service state - see CLAUDE.md's Phase 18 brief, section
    /// 48. Read-only; never used to drive gameplay logic.</summary>
    public readonly struct DeepLinkDiagnostics
    {
        public readonly bool IsReady;
        public readonly int HandlerCount;
        public readonly string PendingRawUri;
        public readonly string LastProcessedRawUri;

        public DeepLinkDiagnostics(bool isReady, int handlerCount, string pendingRawUri, string lastProcessedRawUri)
        {
            IsReady = isReady;
            HandlerCount = handlerCount;
            PendingRawUri = pendingRawUri;
            LastProcessedRawUri = lastProcessedRawUri;
        }
    }
}
