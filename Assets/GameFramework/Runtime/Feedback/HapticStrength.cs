namespace GameFramework.Feedback
{
    /// <summary>Semantic haptic intent — deliberately not raw amplitude/duration, since a specific
    /// mapping is platform/provider-defined (see <see cref="IHapticProvider"/>).</summary>
    public enum HapticStrength
    {
        Light,
        Medium,
        Heavy,
        Success,
        Warning,
        Failure
    }
}
