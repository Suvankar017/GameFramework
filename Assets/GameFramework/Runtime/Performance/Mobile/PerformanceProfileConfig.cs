namespace GameFramework.Performance.Mobile
{
    /// <summary>What applying a <see cref="PerformanceProfile"/> actually sets. Game-configured via
    /// <see cref="IMobilePerformanceService.ConfigureProfile"/> - the framework ships conservative
    /// defaults (see <see cref="MobilePerformanceService"/>) but does not claim they are correct for
    /// any specific game or device.</summary>
    public readonly struct PerformanceProfileConfig
    {
        public readonly int QualityLevel;

        /// <summary>Relative to the device's current resolution, clamped to (0, 1].</summary>
        public readonly float ResolutionScale;

        public readonly int TargetFrameRate;

        public PerformanceProfileConfig(int qualityLevel, float resolutionScale, int targetFrameRate)
        {
            QualityLevel = qualityLevel;
            ResolutionScale = resolutionScale;
            TargetFrameRate = targetFrameRate;
        }
    }
}
