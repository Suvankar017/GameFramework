namespace GameFramework.Notifications
{
    /// <summary>
    /// Deliberately minimal - see CLAUDE.md's Phase 18 brief, section 10 ("do not implement an
    /// unnecessarily complex recurring-event engine"). A provider that cannot honor a repeat mode
    /// reliably should report that limitation explicitly (see <see cref="NotificationResultKind.Unsupported"/>)
    /// rather than silently approximating it.
    /// </summary>
    public enum NotificationRepeatMode
    {
        None,
        Daily,
        Weekly
    }
}
