using System;

namespace GameFramework.PlayerData
{
    /// <summary>
    /// Which automatic-save triggers <see cref="PlayerProfileService"/> reacts to - see
    /// CLAUDE.md's Phase 13 brief, section 46. A game composes exactly the triggers it wants via
    /// <see cref="AutosavePolicy.Triggers"/>; "on milestone" needs no flag of its own, since a
    /// milestone save is simply the game calling <see cref="IPlayerProfileService.Save"/> directly.
    /// </summary>
    [Flags]
    public enum AutosaveTriggers
    {
        /// <summary>No automatic saving - the game must call <see cref="IPlayerProfileService.Save"/>
        /// itself. Section 46 calls this "ManualOnly".</summary>
        None = 0,

        /// <summary>Save when <c>OnApplicationPause(true)</c> fires (the OS backgrounding the app).
        /// The single most important mobile trigger - see CLAUDE.md's Phase 13 brief, section 16.</summary>
        ApplicationPause = 1 << 0,

        /// <summary>Save when <c>OnApplicationFocus(false)</c> fires.</summary>
        FocusLost = 1 << 1,

        /// <summary>Save when <see cref="Runtime.SceneManagement.ISceneService"/> reports a scene
        /// finished loading or unloading.</summary>
        SceneTransition = 1 << 2,

        /// <summary>Save immediately before the active profile is unloaded (including as part of
        /// <see cref="IPlayerProfileService.SwitchProfile"/>), if it is dirty.</summary>
        ProfileUnload = 1 << 3,

        /// <summary>Schedule a debounced save (see <see cref="AutosavePolicy.DebounceSeconds"/>)
        /// whenever any registered section reports itself dirty - see CLAUDE.md's Phase 13 brief,
        /// section 17.</summary>
        DirtyDebounce = 1 << 4,

        All = ApplicationPause | FocusLost | SceneTransition | ProfileUnload | DirtyDebounce
    }
}
