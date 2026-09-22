using System;
using System.Collections.Generic;

namespace GameFramework.Monetization.Entitlements
{
    /// <summary>Persisted cache of owned entitlements - see CLAUDE.md's Phase 15 brief, section 25
    /// and 50: this is a client-side cache for offline/instant-start use, never treated as
    /// authoritative ownership over what a real store/provider would report on restore.</summary>
    [Serializable]
    public sealed class EntitlementSaveData
    {
        [Serializable]
        public sealed class Entry
        {
            public string Id;
            public bool IsOwned;

            /// <summary>Ticks (UTC), or 0 for "no expiration."</summary>
            public long ExpirationUtcTicks;
            public bool IsAutoRenewing;
            public int Source;
        }

        public List<Entry> Entries = new List<Entry>();
    }
}
