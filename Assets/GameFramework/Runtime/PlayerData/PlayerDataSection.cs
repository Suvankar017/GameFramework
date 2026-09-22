using System;
using GameFramework.Runtime.Persistence;

namespace GameFramework.PlayerData
{
    /// <summary>Internal extension point letting <see cref="PlayerProfileService"/> learn the
    /// instant a section is marked dirty, without polling <see cref="IPlayerDataSection.IsDirty"/>
    /// every frame - see CLAUDE.md's Phase 13 brief, section 13/47 ("no per-frame dirty checks").
    /// Only <see cref="PlayerDataSection{TData}"/> implements this; a hand-rolled
    /// <see cref="IPlayerDataSection"/> that doesn't will simply not trigger
    /// <see cref="AutosaveTriggers.DirtyDebounce"/> - it is still saved by every other trigger and
    /// by an explicit <see cref="IPlayerProfileService.Save"/>.</summary>
    internal interface IDirtyNotifyingSection
    {
        Action DirtyNotifier { set; }
    }

    /// <summary>
    /// Base <see cref="IPlayerDataSection"/> for a section backed by one plain, JsonUtility-
    /// compatible data class <typeparamref name="TData"/> - the same constraints
    /// <c>JsonPersistenceSerializer</c> already documents (a <c>[Serializable]</c> type with
    /// public/serialized fields; no properties, interfaces, or polymorphic fields). A game
    /// subclasses this once per section and adds its own domain methods that mutate
    /// <see cref="Data"/> and call <see cref="MarkDirty"/> - see CLAUDE.md's Phase 13 brief,
    /// section 12 ("Strong Data Boundaries"): callers use those domain methods, never
    /// <see cref="Data"/> or <see cref="Runtime.Persistence.IPersistenceService"/> directly.
    ///
    /// <code>
    /// [Serializable]
    /// public sealed class ProgressionSaveData
    /// {
    ///     public List&lt;string&gt; CompletedLevelIds = new List&lt;string&gt;();
    /// }
    ///
    /// public sealed class ProgressionDataSection : PlayerDataSection&lt;ProgressionSaveData&gt;
    /// {
    ///     public override string Id =&gt; "Progression";
    ///     public override int Version =&gt; 1;
    ///
    ///     public bool IsLevelCompleted(string levelId) =&gt; Data.CompletedLevelIds.Contains(levelId);
    ///
    ///     public void SetLevelCompleted(string levelId)
    ///     {
    ///         if (!Data.CompletedLevelIds.Contains(levelId))
    ///         {
    ///             Data.CompletedLevelIds.Add(levelId);
    ///             MarkDirty();
    ///         }
    ///     }
    /// }
    /// </code>
    /// </summary>
    public abstract class PlayerDataSection<TData> : IPlayerDataSection, IDirtyNotifyingSection where TData : class, new()
    {
        private Action _dirtyNotifier;

        /// <summary>The section's current data. Protected, not public - external code goes through
        /// this section's own domain methods (see class remarks), never this property directly.</summary>
        protected TData Data { get; private set; } = new TData();

        public abstract string Id { get; }
        public abstract int Version { get; }
        public bool IsDirty { get; private set; }

        Action IDirtyNotifyingSection.DirtyNotifier { set => _dirtyNotifier = value; }

        /// <summary>Override to build a default instance more complex than <c>new TData()</c>
        /// (e.g. seeding a starting inventory). The default implementation is exactly <c>new TData()</c>.</summary>
        protected virtual TData CreateDefault() => new TData();

        public virtual void ResetToDefaults()
        {
            Data = CreateDefault();
            Validate();
            MarkDirty();
        }

        /// <summary>No-op by default - override to repair/reject invalid state. See
        /// <see cref="IPlayerDataSection.Validate"/>'s remarks.</summary>
        public virtual void Validate()
        {
        }

        /// <summary>Marks this section dirty. Call from a domain mutator after actually changing
        /// <see cref="Data"/> - see class remarks. A no-op call (mutator called but nothing actually
        /// changed) should be avoided by the mutator itself, not by this method, so
        /// <see cref="PlayerProfileService"/>'s dirty-driven autosave only ever schedules for a real
        /// change.</summary>
        protected void MarkDirty()
        {
            IsDirty = true;
            _dirtyNotifier?.Invoke();
        }

        void IPlayerDataSection.Save(IPersistenceService persistence, string storageKey)
        {
            Validate();
            persistence.Save(storageKey, Data, Version);
            IsDirty = false;
        }

        void IPlayerDataSection.Load(IPersistenceService persistence, string storageKey)
        {
            Data = persistence.Load(storageKey, Version, CreateDefault());
            Validate();
            IsDirty = false;
        }

        void IPlayerDataSection.CreateBackup(IPersistenceService persistence, string storageKey, string backupKey)
        {
            if (!persistence.Exists(storageKey))
            {
                return;
            }

            TData current = persistence.Load(storageKey, Version, (TData)null);
            if (current == null)
            {
                return;
            }

            persistence.Save(backupKey, current, Version);
        }
    }
}
