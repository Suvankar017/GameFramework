using GameFramework.Runtime.Services;

namespace GameFramework.Platform
{
    /// <summary>Opens the current game's own store page/review flow - never a generic browser link,
    /// which a caller can already open via <see cref="IPlatformUrlService"/> (see CLAUDE.md's
    /// Phase 14 brief, section 20).</summary>
    public interface IAppStoreService : IGameService
    {
        /// <summary>True if a store identifier is configured for at least one platform (see
        /// <see cref="AppStoreConfig"/>). Does not mean the *current* platform is configured.</summary>
        bool HasConfiguration { get; }

        bool OpenStorePage();

        bool OpenReviewPage();
    }
}
