namespace GameFramework.DeepLinks
{
    public readonly struct DeepLinkResult
    {
        public readonly DeepLinkResultKind Kind;
        public readonly string RawUri;
        public readonly string FailureDetail;

        public bool Success => Kind == DeepLinkResultKind.Handled;

        public DeepLinkResult(DeepLinkResultKind kind, string rawUri, string failureDetail = null)
        {
            Kind = kind;
            RawUri = rawUri;
            FailureDetail = failureDetail;
        }

        public static DeepLinkResult Failure(DeepLinkResultKind kind, string rawUri, string detail) => new DeepLinkResult(kind, rawUri, detail);
    }
}
