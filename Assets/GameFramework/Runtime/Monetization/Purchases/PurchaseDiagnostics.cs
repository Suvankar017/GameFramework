using System.Collections.Generic;

namespace GameFramework.Monetization.Purchases
{
    /// <summary>Read-only development snapshot for a diagnostics menu/overlay - see CLAUDE.md's
    /// Phase 15 brief, section 38.</summary>
    public readonly struct PurchaseDiagnostics
    {
        public readonly MonetizationProviderState State;
        public readonly IReadOnlyList<Product> AvailableProducts;
        public readonly int ProcessedTransactionCount;
        public readonly string LastError;

        public PurchaseDiagnostics(MonetizationProviderState state, IReadOnlyList<Product> availableProducts, int processedTransactionCount, string lastError)
        {
            State = state;
            AvailableProducts = availableProducts;
            ProcessedTransactionCount = processedTransactionCount;
            LastError = lastError;
        }
    }
}
