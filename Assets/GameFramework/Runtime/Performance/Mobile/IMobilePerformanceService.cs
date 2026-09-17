using GameFramework.Runtime.Services;

namespace GameFramework.Performance.Mobile
{
    /// <summary>
    /// Configurable frame-rate/quality/resolution-scale control, so a game never hard-codes
    /// <c>Application.targetFrameRate</c> or <c>QualitySettings.SetQualityLevel</c> at scattered call
    /// sites. Only wraps the handful of settings a mobile game commonly needs - it does not expose
    /// every Unity rendering setting (see the framework documentation's Performance Rules).
    /// </summary>
    public interface IMobilePerformanceService : IGameService
    {
        int TargetFrameRate { get; }

        PerformanceProfile CurrentProfile { get; }

        /// <summary>Direct override, independent of any profile.</summary>
        void SetTargetFrameRate(int frameRate);

        void SetQualityLevel(int qualityLevel);

        /// <summary>Sets the render resolution relative to the device's current resolution via
        /// <see cref="UnityEngine.Screen.SetResolution(int,int,bool)"/>. <paramref name="scale"/> is
        /// clamped to (0, 1].</summary>
        void SetResolutionScale(float scale);

        /// <summary>Replaces the configuration <see cref="ApplyProfile"/> uses for
        /// <paramref name="profile"/>. Call during startup, before <see cref="ApplyProfile"/>.</summary>
        void ConfigureProfile(PerformanceProfile profile, PerformanceProfileConfig config);

        /// <summary>Applies the quality level, resolution scale, and target frame rate configured
        /// for <paramref name="profile"/> (see <see cref="ConfigureProfile"/>).</summary>
        void ApplyProfile(PerformanceProfile profile);
    }
}
