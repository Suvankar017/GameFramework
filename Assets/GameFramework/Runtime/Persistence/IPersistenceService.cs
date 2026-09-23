using GameFramework.Runtime.Services;

namespace GameFramework.Runtime.Persistence
{
    /// <summary>
    /// Save/load for domain-specific data classes the game defines (e.g. <c>PlayerProgressData</c>,
    /// <c>SettingsData</c>) — never for arbitrary Unity objects or whole scenes; see
    /// <c>JsonPersistenceSerializer</c>'s constraints. Save/Load are synchronous (the underlying
    /// JSON serialization and file I/O are synchronous) — there is no SaveAsync/LoadAsync, since
    /// wrapping a synchronous implementation in a Task/coroutine would only pretend to be
    /// asynchronous.
    /// </summary>
    public interface IPersistenceService : IGameService
    {
        bool Exists(string key);

        /// <summary>Serializes and writes <paramref name="data"/> under <paramref name="key"/>,
        /// tagged with <paramref name="version"/> and an integrity checksum. Throws
        /// <see cref="System.ArgumentNullException"/> for null <paramref name="data"/> (a programmer
        /// error - saving "nothing" is <see cref="Delete"/>), and propagates the storage layer's I/O
        /// exception if the write fails - a failed save must be visible to its caller, never silently
        /// dropped. The previously stored value is left intact on failure.</summary>
        void Save<TData>(string key, TData data, int version);

        /// <summary>
        /// Loads the data stored under <paramref name="key"/>. Returns <paramref name="defaultValue"/>
        /// if nothing is stored yet, or if loading fails for any reason (see
        /// <see cref="TryLoad{TData}"/> for the structured outcome) — in which case the failure is
        /// logged and the raw data is kept under <c>"{key}.corrupt"</c> rather than being discarded.
        /// If the stored version is older than <paramref name="currentVersion"/>, walks the
        /// registered <see cref="ISaveMigration"/> chain for <paramref name="key"/> up to it. A
        /// stored version <i>newer</i> than <paramref name="currentVersion"/> is rejected
        /// (<see cref="PersistenceLoadStatus.UnsupportedVersion"/>), never deserialized as if current.
        /// </summary>
        TData Load<TData>(string key, int currentVersion, TData defaultValue);

        /// <summary>
        /// Same pipeline as <see cref="Load{TData}"/> (read → integrity check → version check →
        /// migrate → deserialize), but reports exactly what happened instead of folding every
        /// failure into "returned the default". <paramref name="data"/> is only meaningful when the
        /// result <see cref="PersistenceLoadStatusExtensions.IsSuccess"/>; otherwise it is
        /// <c>default</c>. Never throws for bad stored data - only for a null/empty key.
        /// </summary>
        PersistenceLoadStatus TryLoad<TData>(string key, int currentVersion, out TData data);

        /// <summary>Deletes the data stored under <paramref name="key"/>, if any. Also the
        /// mechanism for "reset to defaults" — the next <see cref="Load{TData}"/> for that key
        /// naturally returns its default.</summary>
        void Delete(string key);

        /// <summary>Registers one version-chain step for <paramref name="key"/>. Throws if a
        /// migration from the same <see cref="ISaveMigration.FromVersion"/> is already registered
        /// for that key.</summary>
        void RegisterMigration(string key, ISaveMigration migration);
    }
}
