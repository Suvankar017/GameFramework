using GameFramework.Runtime.Services;

namespace GameFramework.Platform
{
    /// <summary>
    /// Platform identity - the one place framework/game code asks "what platform is this," so
    /// gameplay/UI code never needs its own <c>#if UNITY_ANDROID</c>/<c>UNITY_IOS</c> or
    /// <c>UnityEngine.Application.platform</c> switch (see CLAUDE.md's Phase 14 brief, section 3).
    /// </summary>
    public interface IPlatformService : IGameService
    {
        PlatformType Platform { get; }

        bool IsEditor { get; }
        bool IsMobile { get; }
        bool IsAndroid { get; }
        bool IsIOS { get; }
        bool IsDesktop { get; }
    }
}
