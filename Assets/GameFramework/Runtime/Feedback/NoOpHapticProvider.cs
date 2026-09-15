namespace GameFramework.Feedback
{
    /// <summary>Used automatically outside mobile platforms (Editor, desktop) so requesting
    /// haptics there is a safe no-op rather than an error — see <see cref="FeedbackService"/>.</summary>
    public sealed class NoOpHapticProvider : IHapticProvider
    {
        public bool IsSupported => false;

        public void Trigger(HapticStrength strength)
        {
        }
    }
}
