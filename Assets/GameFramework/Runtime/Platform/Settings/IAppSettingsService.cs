using GameFramework.Runtime.Services;

namespace GameFramework.Platform
{
    /// <summary>Opens the OS-level settings screen for this application (e.g. so a player who
    /// denied a permission can grant it manually) - not a game-settings framework, which remains
    /// Phase 2's <c>Runtime.Settings.ISettingsService</c> (see CLAUDE.md's Phase 14 brief,
    /// section 36).</summary>
    public interface IAppSettingsService : IGameService
    {
        bool OpenApplicationSettings();
    }
}
