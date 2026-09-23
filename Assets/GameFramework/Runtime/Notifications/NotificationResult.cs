namespace GameFramework.Notifications
{
    public readonly struct NotificationResult
    {
        public readonly NotificationResultKind Kind;
        public readonly NotificationId Id;
        public readonly string FailureDetail;

        public bool Success => Kind == NotificationResultKind.Success;

        public NotificationResult(NotificationResultKind kind, NotificationId id, string failureDetail = null)
        {
            Kind = kind;
            Id = id;
            FailureDetail = failureDetail;
        }

        public static NotificationResult Ok(NotificationId id) => new NotificationResult(NotificationResultKind.Success, id);
        public static NotificationResult Failure(NotificationResultKind kind, NotificationId id, string detail = null) => new NotificationResult(kind, id, detail);
    }
}
