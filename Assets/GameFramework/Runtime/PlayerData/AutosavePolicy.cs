namespace GameFramework.PlayerData
{
    /// <summary>
    /// Configures <see cref="PlayerProfileService"/>'s automatic-save behavior. A game assigns its
    /// own instance to <see cref="IPlayerProfileService.AutosavePolicy"/> before or after loading a
    /// profile; the default (<see cref="AutosaveTriggers.All"/>, a 3 second debounce) is a
    /// reasonable mobile-safe default, not a mandate - see CLAUDE.md's Phase 13 brief, section 46.
    /// </summary>
    public sealed class AutosavePolicy
    {
        public AutosaveTriggers Triggers = AutosaveTriggers.All;

        /// <summary>How long to wait after the last dirty-marking mutation before an
        /// <see cref="AutosaveTriggers.DirtyDebounce"/> save actually runs - see CLAUDE.md's Phase
        /// 13 brief, section 17. Each new dirty mutation reschedules this timer rather than queuing
        /// an additional save, so a burst of rapid changes (five coin pickups in a row) produces one
        /// write, not five.</summary>
        public float DebounceSeconds = 3f;

        public static AutosavePolicy Default => new AutosavePolicy();

        public static AutosavePolicy ManualOnly => new AutosavePolicy { Triggers = AutosaveTriggers.None };
    }
}
