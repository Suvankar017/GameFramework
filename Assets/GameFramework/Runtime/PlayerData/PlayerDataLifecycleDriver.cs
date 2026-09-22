using UnityEngine;

namespace GameFramework.PlayerData
{
    /// <summary>
    /// Private <see cref="PlayerProfileService"/> helper that forwards
    /// <c>OnApplicationPause</c>/<c>OnApplicationFocus</c>/<c>OnApplicationQuit</c> so the service
    /// can flush a dirty profile at safe mobile lifecycle points - see CLAUDE.md's Phase 13 brief,
    /// section 16.
    ///
    /// This is deliberately not a reference to <c>Performance.Mobile.IApplicationLifecycleService</c>
    /// (Phase 5), which already centralizes exactly these three callbacks as republished events:
    /// <see cref="GameFramework.PlayerData"/> keeps the same "usable by any game regardless of which
    /// other systems it also uses" independence Phase 5 itself established for
    /// <c>GameFramework.Performance</c> (references only <c>GameFramework.Core</c>/
    /// <c>GameFramework.Runtime</c> - see this assembly's own asmdef) - a game does not have to pull
    /// in Performance's profiling/tick/pooling/mobile-quality surface just to get profile autosave.
    /// This driver never touches <c>Runtime.Time.ITimeService</c> and never republishes a public
    /// event; it exists purely to trigger a save, which is a narrower and different concern than
    /// what <see cref="GameFramework.Performance.Mobile.ApplicationLifecycleService"/> does
    /// (owning gameplay time-scale pause) - so the two do not compete as "the same system twice."
    /// If a game also uses Phase 5, both react to the same underlying Unity callback independently
    /// and harmlessly, exactly as two unrelated <c>MonoBehaviour</c>s in the same scene already would.
    /// </summary>
    internal sealed class PlayerDataLifecycleDriver : MonoBehaviour
    {
        public PlayerProfileService Owner;

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Owner?.HandleApplicationPaused();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Owner?.HandleFocusLost();
            }
        }

        private void OnApplicationQuit()
        {
            Owner?.HandleApplicationQuitting();
        }
    }
}
