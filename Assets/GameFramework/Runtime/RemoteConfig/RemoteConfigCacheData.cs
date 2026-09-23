using System;
using System.Collections.Generic;

namespace GameFramework.RemoteConfig
{
    /// <summary>
    /// Persisted cache payload - see CLAUDE.md's Phase 17 brief, sections 28/31. Stored through
    /// <see cref="GameFramework.Runtime.Persistence.IPersistenceService"/> under its own key, entirely
    /// separate from player-save data (section 31: "do not place remote configuration into player-save
    /// files"). A plain list of entries (rather than a dictionary) because Unity's JSON serializer
    /// cannot serialize <see cref="Dictionary{TKey,TValue}"/> directly - the same reason
    /// <c>EntitlementSaveData</c>/<c>PurchaseSaveData</c> use a list.
    /// </summary>
    [Serializable]
    public sealed class RemoteConfigCacheData
    {
        public int Version;
        public int SchemaVersion;
        public string Environment = string.Empty;
        public long FetchedAtUtcTicks;
        public List<Entry> Entries = new List<Entry>();

        [Serializable]
        public sealed class Entry
        {
            public string Key = string.Empty;
            public RemoteConfigTypedValue Value;
        }
    }
}
