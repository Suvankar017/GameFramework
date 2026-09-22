namespace GameFramework.Monetization
{
    /// <summary>
    /// Shared initialization state for <see cref="Ads.IAdsService"/> and
    /// <see cref="Purchases.IPurchaseService"/> - one explicit state model instead of the several
    /// unrelated booleans (isInitialized/isLoading/isReady/hasError) CLAUDE.md's Phase 15 brief
    /// (section 33) warns against.
    /// </summary>
    public enum MonetizationProviderState
    {
        NotInitialized,
        Initializing,
        Initialized,

        /// <summary>The provider reported it cannot be used on this platform/build (e.g. no
        /// network, unsupported store) - distinct from <see cref="Failed"/>, which is an
        /// initialization error.</summary>
        Unavailable,

        Failed
    }
}
