using GameFramework.Runtime.Services;

namespace GameFramework.Platform
{
    /// <summary>Safe, validated <see cref="UnityEngine.Application.OpenURL"/> wrapper - the
    /// framework never hard-codes a URL (see CLAUDE.md's Phase 14 brief, section 19); callers supply
    /// their own.</summary>
    public interface IPlatformUrlService : IGameService
    {
        /// <summary>Returns false without opening anything if <paramref name="url"/> is null/empty
        /// or not a well-formed absolute URI (see <see cref="PlatformUrlService.IsValidUrl"/>).</summary>
        bool OpenUrl(string url);
    }
}
