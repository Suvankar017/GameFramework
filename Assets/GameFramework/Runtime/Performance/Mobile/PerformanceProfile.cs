namespace GameFramework.Performance.Mobile
{
    /// <summary>
    /// A configured performance tier a game applies via <see cref="IMobilePerformanceService.ApplyProfile"/> -
    /// game-authored configuration, not a hard-coded judgment of any specific device. Nothing in
    /// this framework picks one of these automatically from <see cref="DeviceInfo"/>; a game decides
    /// that mapping itself (or lets the player choose) and calls <see cref="IMobilePerformanceService.ApplyProfile"/>
    /// with the result.
    /// </summary>
    public enum PerformanceProfile
    {
        Low,
        Medium,
        High
    }
}
