using System;
using GameFramework.Runtime.Services;

namespace GameFramework.Platform
{
    /// <summary>
    /// Answers "is it granted" and "can I ask" - the game decides when and why to ask (see
    /// CLAUDE.md's Phase 14 brief, section 35). Never shows an OS prompt on its own; only
    /// <see cref="RequestPermission"/>, called explicitly by game code, can do that.
    /// </summary>
    public interface IPermissionService : IGameService
    {
        PermissionStatus GetStatus(PlatformPermission permission);

        bool CanRequest(PlatformPermission permission);

        /// <summary><paramref name="onResult"/> is invoked exactly once, with the resulting status.
        /// Invoked synchronously, with <see cref="PermissionStatus.Granted"/>, if the permission is
        /// already granted; otherwise invoked after the OS prompt closes.</summary>
        void RequestPermission(PlatformPermission permission, Action<PermissionStatus> onResult);
    }
}
