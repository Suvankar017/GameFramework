using System.Collections.Generic;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Performance.Mobile
{
    /// <summary>Default <see cref="IMobilePerformanceService"/>. Ships one conservative default
    /// <see cref="PerformanceProfileConfig"/> per <see cref="PerformanceProfile"/>, derived from
    /// however many quality levels the project actually has configured (never a hard-coded index
    /// count) - override via <see cref="ConfigureProfile"/> before calling <see cref="ApplyProfile"/>
    /// if a game wants different values.</summary>
    public sealed class MobilePerformanceService : IMobilePerformanceService
    {
        private readonly Dictionary<PerformanceProfile, PerformanceProfileConfig> _profiles =
            new Dictionary<PerformanceProfile, PerformanceProfileConfig>();

        public int TargetFrameRate { get; private set; }

        public PerformanceProfile CurrentProfile { get; private set; } = PerformanceProfile.Medium;

        public void Initialize(IServiceRegistry registry)
        {
            int qualityLevelCount = QualitySettings.names.Length;
            int lowest = 0;
            int highest = Mathf.Max(0, qualityLevelCount - 1);
            int middle = Mathf.Clamp(qualityLevelCount / 2, lowest, highest);

            _profiles[PerformanceProfile.Low] = new PerformanceProfileConfig(lowest, 0.75f, 30);
            _profiles[PerformanceProfile.Medium] = new PerformanceProfileConfig(middle, 0.9f, 30);
            _profiles[PerformanceProfile.High] = new PerformanceProfileConfig(highest, 1f, 60);

            TargetFrameRate = Application.targetFrameRate;
        }

        public void Shutdown()
        {
            _profiles.Clear();
        }

        public void SetTargetFrameRate(int frameRate)
        {
            TargetFrameRate = frameRate;
            Application.targetFrameRate = frameRate;
        }

        public void SetQualityLevel(int qualityLevel)
        {
            QualitySettings.SetQualityLevel(qualityLevel, applyExpensiveChanges: true);
        }

        public void SetResolutionScale(float scale)
        {
            scale = Mathf.Clamp(scale, 0.25f, 1f);
            Resolution current = Screen.currentResolution;
            int width = Mathf.RoundToInt(current.width * scale);
            int height = Mathf.RoundToInt(current.height * scale);
            Screen.SetResolution(width, height, Screen.fullScreen);
        }

        public void ConfigureProfile(PerformanceProfile profile, PerformanceProfileConfig config)
        {
            _profiles[profile] = config;
        }

        public void ApplyProfile(PerformanceProfile profile)
        {
            if (!_profiles.TryGetValue(profile, out PerformanceProfileConfig config))
            {
                return;
            }

            CurrentProfile = profile;
            SetQualityLevel(config.QualityLevel);
            SetResolutionScale(config.ResolutionScale);
            SetTargetFrameRate(config.TargetFrameRate);
        }
    }
}
