using System;
using System.Collections.Generic;

namespace GameFramework.Monetization.Purchases
{
    /// <summary>
    /// Persisted idempotency record - see CLAUDE.md's Phase 15 brief, section 41. Guarantee level:
    /// a transaction id is recorded synchronously in memory the instant it is processed and flushed
    /// to disk on the same Save cadence every other framework service uses (dirty-flag +
    /// <c>Shutdown</c>/explicit <see cref="PurchaseService.Save"/>) - this is "won't double-grant
    /// across ordinary app restarts," not an atomic, crash-mid-write-safe transaction log (Phase 2's
    /// <c>IPersistenceService</c> offers no such primitive). Documented explicitly rather than
    /// implying a stronger guarantee than the framework actually provides.
    /// </summary>
    [Serializable]
    public sealed class PurchaseSaveData
    {
        public List<string> ProcessedTransactionIds = new List<string>();
    }
}
