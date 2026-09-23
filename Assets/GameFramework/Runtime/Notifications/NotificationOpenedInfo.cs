namespace GameFramework.Notifications
{
    /// <summary>Reported by a provider when a notification is opened (tapped) or received while the
    /// app is running - see CLAUDE.md's Phase 18 brief, sections 16/21. <see cref="WasSimulated"/>
    /// distinguishes an Editor/Mock-simulated event from one a real platform actually delivered
    /// (section 16 - never pretend the two are the same).</summary>
    public readonly struct NotificationOpenedInfo
    {
        public readonly NotificationId Id;
        public readonly NotificationPayload Payload;
        public readonly bool WasSimulated;

        public NotificationOpenedInfo(NotificationId id, NotificationPayload payload, bool wasSimulated)
        {
            Id = id;
            Payload = payload ?? NotificationPayload.Empty;
            WasSimulated = wasSimulated;
        }
    }
}
